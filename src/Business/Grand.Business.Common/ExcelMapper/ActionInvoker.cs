using System;

namespace Ganss.Excel
{
    /// <summary>
    /// Abstract class action invoker
    /// </summary>
    internal class ActionInvoker
    {
        /// <summary>
        /// Invoke from an unspecified <paramref name="obj"/> type
        /// </summary>
        /// <param name="obj">mapping instance class</param>
        /// <param name="index">index in the collection</param>
        internal virtual void Invoke(object obj, int index) =>
            throw new NotImplementedException();

        /// <summary>
        /// <see cref="ActionInvokerImpl{T}"/> factory
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mappingAction"></param>
        /// <returns></returns>
        internal static ActionInvoker CreateInstance<T>(Action<T, int> mappingAction)
        {
            // instantiate concrete generic invoker
            var invokerType = typeof(ActionInvokerImpl<>);
            Type[] tType = { typeof(T) };
            Type constructed = invokerType.MakeGenericType(tType);
            object invokerInstance = Activator.CreateInstance(constructed, mappingAction);
            return (ActionInvoker)invokerInstance;
        }
    }

    /// <summary>
    /// Generic form <see cref="ActionInvoker"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ActionInvokerImpl<T> : ActionInvoker
        where T : class
    {
        /// <summary>
        /// ref to the mapping action.
        /// </summary>
        internal Action<T, int> mappingAction;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="mappingAction"></param>
        public ActionInvokerImpl(Action<T, int> mappingAction)
        {
            this.mappingAction = mappingAction;
        }

        /// <summary>
        /// Invoke Generic Action
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="index"></param>
        internal override void Invoke(object obj, int index)
        {
            if (mappingAction is null || obj is null) return;
            mappingAction((obj as T), index);
        }
    }
}