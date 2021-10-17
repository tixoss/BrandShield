using Autofac;
using Autofac.Integration.WebApi;
using log4net;
using Owin;
using ServiceApplication.Services;
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

            builder.Register(ctx => LogManager.GetLogger(typeof(Startup).Assembly.GetName().Name))
                .As<ILog>();

            builder.Register(ctx => CloudExecuterFactory.GetInstance(Consts.LIMIT_MACHINES, LogManager.GetLogger(typeof(CloudExecuterFactory))))
                .As<ICloudExecuterFactory>()
                .SingleInstance();
            builder.Register(ctx => ExecuterRegister.GetInstance(ctx.Resolve<ICloudExecuterFactory>(), LogManager.GetLogger(typeof(ExecuterRegister))))
                .As<IExecuterRegister>()
                .SingleInstance();

            builder.RegisterType<TranslationService>().As<ITranslationService>();

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
    }
}
