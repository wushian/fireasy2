﻿// -----------------------------------------------------------------------
// <copyright company="Fireasy"
//      email="faib920@126.com"
//      qq="55570729">
//   (c) Copyright Fireasy. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
using Fireasy.Data.Entity.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;

namespace Fireasy.Data.Entity
{
    /// <summary>
    /// 提供以对象形式查询和使用实体数据的功能。
    /// </summary>
    public abstract class EntityContext : IDisposable
    {
        private InternalContext context;
        private bool isDisposed;
        private bool isBeginTransaction = false;

        /// <summary>
        /// 初始化 <see cref="EntityContext"/> 类的新实例。
        /// </summary>
        public EntityContext()
            : this(string.Empty)
        {
        }

        /// <summary>
        /// 使用一个配置名称来初始化 <see cref="EntityContext"/> 类的新实例。
        /// </summary>
        /// <param name="instanceName">实例名称。</param>
        public EntityContext(string instanceName)
            : this (new EntityContextOptions(instanceName))
        {
        }

        /// <summary>
        /// 初始化 <see cref="EntityContext"/> 类的新实例。
        /// </summary>
        /// <param name="options">选项参数。</param>
        public EntityContext(EntityContextOptions options)
        {
            Initialize(options);

            new EntityRepositoryDiscoveryService(this).InitializeSets();
        }

        /// <summary>
        /// 析构函数。
        /// </summary>
        ~EntityContext()
        {
            Dispose(false);
        }

        /// <summary>
        /// 获取关联的 <see cref="IDatabase"/> 对象。
        /// </summary>
        public IDatabase Database
        {
            get { return context.Database; }
        }

        /// <summary>
        /// 销毁资源。
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 销毁资源。
        /// </summary>
        /// <param name="disposing">如果为 true，则同时释放托管资源和非托管资源；如果为 false，则仅释放非托管资源。</param>
        protected void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (context != null)
                {
                    if (isBeginTransaction)
                    {
                        context.Database.RollbackTransaction();
                    }

                    context.Dispose();
                }

                isDisposed = true;
            }
        }

        /// <summary>
        /// 为指定的类型返回 <see cref="EntityRepository{TEntity}"/>
        /// </summary>
        /// <typeparam name="TEntity">实体类型。</typeparam>
        /// <returns></returns>
        public EntityRepository<TEntity> Set<TEntity>() where TEntity : IEntity
        {
            Type entitytype = typeof(TEntity);
            return context.GetDbSet(entitytype) as EntityRepository<TEntity>;
        }

        /// <summary>
        /// 为指定的类型返回 <see cref="IRepository"/>
        /// </summary>
        /// <param name="entitytype">实体类型。</param>
        /// <returns></returns>
        public IRepository Set(Type entitytype)
        {
            return context.GetDbSet(entitytype);
        }

        /// <summary>
        /// 创建树实体仓储实例。
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public ITreeRepository<TEntity> CreateTreeRepository<TEntity>() where TEntity : class, IEntity
        {
            var repository = Set<TEntity>();
            return new EntityTreeRepository<TEntity>(repository, context);
        }

        /// <summary>
        /// 指定要包括在查询结果中的关联对象。
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="fnMember">要包含的属性的表达式。</param>
        /// <returns></returns>
        public EntityContext Include<TEntity>(Expression<Func<TEntity, object>> fnMember) where TEntity : IEntity
        {
            context.IncludeWith(fnMember);
            return this;
        }

        /// <summary>
        /// 对关联对象
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memberQuery"></param>
        /// <returns></returns>
        public EntityContext Associate<TEntity>(Expression<Func<TEntity, IEnumerable>> memberQuery) where TEntity : IEntity
        {
            context.AssociateWith(memberQuery);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="fnApply"></param>
        /// <returns></returns>
        public EntityContext Apply<TEntity>(Expression<Func<IEnumerable<TEntity>, IEnumerable<TEntity>>> fnApply) where TEntity : IEntity
        {
            context.Apply(fnApply);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="fnApply"></param>
        /// <returns></returns>
        public EntityContext Apply(Type entityType, LambdaExpression fnApply)
        {
            context.Apply(entityType, fnApply);
            return this;
        }

        /// <summary>
        /// 配置参数。
        /// </summary>
        /// <param name="configuration">配置参数的方法。</param>
        /// <returns></returns>
        public EntityContext ConfigOptions(Action<EntityContextOptions> configuration)
        {
            configuration?.Invoke(context.Options);

            return this;
        }

        /// <summary>
        /// 开始事务。
        /// </summary>
        /// <param name="level"></param>
        public void BeginTransaction(IsolationLevel level = IsolationLevel.ReadCommitted)
        {
            if (context.Database.BeginTransaction())
            {
                isBeginTransaction = true;
            }
        }

        /// <summary>
        /// 提交事务。
        /// </summary>
        public void CommitTransaction()
        {
            if (context.Database.CommitTransaction())
            {
                isBeginTransaction = false;
            }
        }

        /// <summary>
        /// 回滚事务。
        /// </summary>
        public void RollbackTransaction()
        {
            if (context.Database.RollbackTransaction())
            {
                isBeginTransaction = false;
            }
        }

        /// <summary>
        /// 初始化。
        /// </summary>
        private void Initialize(EntityContextOptions options)
        {
            context = new InternalContext(options)
                {
                    OnRespositoryCreated = OnRespositoryCreated,
                    OnRespositoryCreateFailed = OnRespositoryCreateFailed
            };
        }

        /// <summary>
        /// 仓储创建时可进行数据初始化。
        /// </summary>
        /// <param name="entityType">实体类型。</param>
        protected virtual void OnRespositoryCreated(Type entityType)
        {
        }

        /// <summary>
        /// 仓储创建失败时通知。
        /// </summary>
        /// <param name="entityType">实体类型。</param>
        /// <param name="exception">触发的异常。</param>
        protected virtual void OnRespositoryCreateFailed(Type entityType, Exception exception)
        {
        }
    }
}
