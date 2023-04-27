using System;
using System.Threading;
using System.Threading.Tasks;

namespace Das.Serializer.Concurrency;

public static class InvocationBase
{
   // ReSharper disable once UnusedMember.Global
   public static void RunSync(Func<Task> task)
   {
      var oldContext = SynchronizationContext.Current;
      var synch = new ExclusiveSynchronizationContext();
      SynchronizationContext.SetSynchronizationContext(synch);
      // ReSharper disable once AsyncVoidLambda
      synch.Post(async _ =>
      {
         try
         {
            await task();
         }
         catch (Exception e)
         {
            synch.InnerException = e;
            throw;
         }
         finally
         {
            synch.EndMessageLoop();
         }
      }, null!);
      synch.BeginMessageLoop();

      SynchronizationContext.SetSynchronizationContext(oldContext);
   }

   // ReSharper disable once UnusedMember.Global
   public static T RunSync<T>(Func<Task<T>> task)
   {
      var oldContext = SynchronizationContext.Current;
      var synch = new ExclusiveSynchronizationContext();
      SynchronizationContext.SetSynchronizationContext(synch);
      var ret = default(T);
      // ReSharper disable once AsyncVoidLambda
      synch.Post(async _ =>
      {
         try
         {
            ret = await task();
         }
         catch (Exception e)
         {
            synch.InnerException = e;
            throw;
         }
         finally
         {
            synch.EndMessageLoop();
         }
      }, null!);
      synch.BeginMessageLoop();
      SynchronizationContext.SetSynchronizationContext(oldContext);
      return ret!;
   }
}