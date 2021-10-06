namespace doki_theme_visualstudio {
  public static class Extensions {

    public static string ToHexString(this  System.Drawing.Color color){
      return color.Name.Substring(2);
    }
  }
}
