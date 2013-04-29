using Rhino;
using Rhino.Commands;

namespace Insert
{
  [System.Runtime.InteropServices.Guid("429ee779-bef6-42f5-9c48-bcd33f46c689")]
  public class InsertCommand : Command
  {
    public InsertCommand()
    {
      // Rhino only creates one instance of each command class defined in a
      // plug-in, so it is safe to store a reference in a static property.
      Instance = this;
    }

    ///<summary>The only instance of this command.</summary>
    public static InsertCommand Instance
    {
      get;
      private set;
    }

    ///<returns>The command name as it appears on the Rhino command line.</returns>
    public override string EnglishName
    {
      get { return "NewInsert"; }
    }

    static ViewModels.InsertInstanceCommandOptions g_DefaultCommandOptions = new ViewModels.InsertInstanceCommandOptions();
    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
      // View Model to associate with this instance of RunCommand, this
      // View Model is used by both the scripting and interactive versions
      // of the command.
      var model = new ViewModels.InsertCommandViewModel(doc, g_DefaultCommandOptions, false);
      // Run the scripting or GUI methods to gather data in the View Model.
      var result = (mode == RunMode.Scripted ? RunScript(model) : RunInteractive(model));
      if (result != Result.Success)
        return result;
      // If a point was successfully added to the model return Success
      // otherwise return Failure
      return Result.Success;
    }
    /// <summary>
    /// Called when the command is in interactive (window) mode
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    static Result RunInteractive(ViewModels.InsertCommandViewModel model)
    {
      var result = Result.Cancel;

      #region Windows Specific UI
      #if ON_OS_WINDOWS
      var window = new Win.InsertCommandWindow();
      model.Window = window;
      // Attach the view model to the window
      window.DataContext = model;
      // Need to set the Rhino main frame window as the parent
      // for the new window otherwise the window will go behind
      // the main frame when the Rhino is deactivated then 
      // activated again.
      // http://blogs.msdn.com/b/mhendersblog/archive/2005/10/04/476921.aspx
      var interopHelper = new System.Windows.Interop.WindowInteropHelper(window);
      interopHelper.Owner = Rhino.RhinoApp.MainWindowHandle();
      window.ShowDialog();
      result = (true == window.DialogResult ? Result.Success : Result.Cancel);
      #endif
      #endregion

      #region Mac Specific UI

      #if ON_OS_MAC
      // Create a NSWindow from a Nib file
      var window = RhinoMac.Window.FromNib("InsertWindow", model);
      // Associate the window with the View Model so that the
      // model's Okay and Cancel methods can close the window
      model.Window = window;
      // Display the window
      window.ShowModal();
      // Success will be true if the window was closed by the
      // OK button otherwise it should be false.
      result = (window.DialogResult == true ? Result.Success : Result.Cancel);
      #endif
      #endregion

      return result;
    }
    /// <summary>
    /// Called when the command is in scripted mode
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    static Result RunScript(ViewModels.InsertCommandViewModel model)
    {
      // Prompt for point on the Rhino command line.
      Rhino.Geometry.Point3d point;
      var result = Rhino.Input.RhinoGet.GetPoint("Point location", false, out point);
      return result;
    }
  }
}
