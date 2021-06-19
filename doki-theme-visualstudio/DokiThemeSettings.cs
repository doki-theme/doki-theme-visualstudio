using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace doki_theme_visualstudio {
  class DokiThemeSettings : DialogPage {
    bool helloWorld = true;
    public bool HelloWorld {
      get { return helloWorld; }
      set { helloWorld = value; }
    }
  }
}
