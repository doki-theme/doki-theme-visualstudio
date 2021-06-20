using System;
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
  }
}
