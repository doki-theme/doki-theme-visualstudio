using System;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;


namespace doki_theme_visualstudio {
  public class ToolBox {
    public static void RunSafely(Action runnable, Action<Exception> errorHandler) {
      try {
        runnable();
      }
      catch (Exception e) {
        errorHandler.Invoke(e);
      }
    }

    public static async Task RunSafelyAsync(Func<Task> runnable, Action<Exception> errorHandler) {
      try {
        await runnable();
      }
      catch (Exception e) {
        errorHandler.Invoke(e);
      }
    }

    public static async Task<T> RunSafelyWithResultAsync<T>(Func<Task<T>> runnable, Func<Exception, T> errorHandler) {
      try {
        return await runnable();
      }
      catch (Exception e) {
        return errorHandler.Invoke(e);
      }
    }

    public static T RunSafelyWithResult<T>(Func<T> runnable, Func<Exception, T> errorHandler) {
      try {
        return runnable();
      }
      catch (Exception e) {
        return errorHandler.Invoke(e);
      }
    }
  }
}
