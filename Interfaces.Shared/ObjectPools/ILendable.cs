using System;

namespace Das.Serializer
{

    public interface ILendable<out T, in TCTorParam1, in TCTorParam2, in TCTorParam3> 
        : ILendable<T> where T : ILendable<T>
    {
        /// <summary>
        /// A method to initialize/re-initialize the pooled object
        /// </summary>

        void Construct(TCTorParam1 param1, TCTorParam2 param2, TCTorParam3 param3);
    }


    /// <summary>
    /// Poolable object using a "constructor" with one parameter
    /// </summary>
    public interface ILendable<out T, in TCTorParam1,
        in TCTorParam2> : ILendable<T> where T : ILendable<T>
    {
        /// <summary>
        /// A method to initialize/re-initialize the pooled object
        /// </summary>
		
        void Construct(TCTorParam1 param1, TCTorParam2 param2);
    }

    /// <summary>
    /// Poolable object using a "constructor" with one parameter
    /// </summary>
	
    public interface ILendable<out T, in TCTorParam> : ILendable<T> where T : ILendable<T>
    {
        /// <summary>
        /// A method to initialize/re-initialize the pooled object
        /// </summary>
		
        void Construct(TCTorParam input);
    }

    /// <summary>
    /// Poolable object using a default constructor
    /// </summary>
	
    public interface ILendable<out T> : IDisposable 
        where T : ILendable<T>
    {		
        Action<T> ReturnToSender { set; }
    }
}