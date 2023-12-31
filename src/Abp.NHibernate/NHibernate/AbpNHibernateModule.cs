﻿using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.Modules;
using Abp.NHibernate.Configuration;
using Abp.NHibernate.EventListeners;
using Abp.NHibernate.Filters;
using Abp.NHibernate.Interceptors;
using Abp.NHibernate.Repositories;
using Abp.NHibernate.Uow;
using NHibernate;
using NHibernate.Event;
using System.Reflection;

namespace Abp.NHibernate
{
    /// <summary>
    /// This module is used to implement "Data Access Layer" in NHibernate.
    /// </summary>
    [DependsOn(typeof(AbpKernelModule))]
    public class AbpNHibernateModule : AbpModule
    {
        /// <summary>
        /// NHibernate session factory object.
        /// </summary>
        private ISessionFactory _sessionFactory;

        public override void PreInitialize()
        {
            IocManager.Register<IAbpNHibernateModuleConfiguration, AbpNHibernateModuleConfiguration>();
            Configuration.ReplaceService<IUnitOfWorkFilterExecuter, NhUnitOfWorkFilterExecuter>(DependencyLifeStyle.Transient);
        }

        /// <inheritdoc/>
        public override void Initialize()
        {
            IocManager.Register<AbpNHibernateInterceptor>(DependencyLifeStyle.Transient);
            IocManager.Register<AbpNHibernateDeleteEventListener>(DependencyLifeStyle.Transient);
            IocManager.Register<AbpNHibernateLoadEventListener>(DependencyLifeStyle.Transient);

            _sessionFactory = Configuration.Modules.AbpNHibernate().FluentConfiguration
                .Mappings(m => m.FluentMappings.Add(typeof(SoftDeleteFilter)))
                .Mappings(m => m.FluentMappings.Add(typeof(MayHaveTenantFilter)))
                .Mappings(m => m.FluentMappings.Add(typeof(MustHaveTenantFilter)))
                .ExposeConfiguration(config =>
                {
                    config.SetListener(ListenerType.Delete, IocManager.Resolve<AbpNHibernateDeleteEventListener>());
                    config.SetListener(ListenerType.Load, IocManager.Resolve<AbpNHibernateLoadEventListener>());

                    config.SetInterceptor(IocManager.Resolve<AbpNHibernateInterceptor>());
                })
                .BuildSessionFactory();

            IocManager.IocContainer.Install(new NhRepositoryInstaller(_sessionFactory));
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
        }

        /// <inheritdoc/>
        public override void Shutdown()
        {
            _sessionFactory.Dispose();
        }
    }
}
