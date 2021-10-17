using Autofac;
using Autofac.Integration.WebApi;
using Owin;
using ServiceApplication.Services;
using ServiceApplication.Services.Impls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace ServiceApplication
{
    public class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();

            RegisterDependencies(config);

            RegisterRoutes(config);

            appBuilder.UseWebApi(config);
        }

        private void RegisterDependencies(HttpConfiguration config)
        {
            var builder = new ContainerBuilder();
            builder.RegisterApiControllers(typeof(Startup).Assembly);

            // ...
            builder.Register(ctx => CloudExecuterFactory.GetInstance(Consts.LIMIT_MACHINES))
                .As<ICloudExecuterFactory>()
                .SingleInstance();
            builder.Register(ctx => BuildFreeExecuterHolder(ctx, Consts.ALWAYS_LIVE_MACHINES))
                .As<IFreeExecuterHolder>()
                .SingleInstance();

            builder.RegisterType<FakeTranslationService>().As<ITranslationService>();

            var container = builder.Build();
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }

        private void RegisterRoutes(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }

        private static IFreeExecuterHolder BuildFreeExecuterHolder(IComponentContext ctx, int count)
        {
            var factory = ctx.Resolve<ICloudExecuterFactory>();
            try
            {
                var executerConnectionTask = BuildBunchLiveExecuters(factory, count).Select(x => x.AsTask()).ToArray();

                Task.WhenAll(executerConnectionTask);

                return new FreeExecuterHolder(Consts.ALWAYS_LIVE_MACHINES, executerConnectionTask.Select(x => x.Result));
            }
            catch (Exception ex)
            {
                throw new Exception("Application initialization failed", ex);
            }
        }

        private static IEnumerable<ValueTask<IExecuterConnection>> BuildBunchLiveExecuters(ICloudExecuterFactory factory, int count)
        {
            for (var i = 0; i < count; i++)
            {
                yield return factory.GetNewAsync();
            }
        }
    }
}
