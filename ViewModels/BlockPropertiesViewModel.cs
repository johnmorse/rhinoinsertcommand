using System;

using Rhino.DocObjects;

namespace Insert.ViewModels
{
  class BlockPropertiesViewModel : InsertBaseViewModel
  {
    public BlockPropertiesViewModel(Rhino.RhinoDoc doc, string fileName, InsertInstanceCommandOptions options)
    : base(doc)
    {
      // Create a new set of command arguments from the options passed to the constructor
      CommandArgs = new InsertInstanceCommandOptions(options);

      // Make sure the docId is set to the requested document
      CommandArgs.DocumentId = options.DocumentId;

      // Use the default layer style, gets set to None if the default update style is other than "Linked"
      CommandArgs.LayerStyle = InsertInstanceOptions.Defaults.LayerStyle;

      ExistingInstanceDefinitions = CommandArgs.InstanceDefintionsFromFileName(fileName);

      // Cook up the block name from the file name looking for existing matches in the process
      InstanceDefinition found;
      string block;
      CommandArgs.BlockNameFromFileName(fileName, ExistingInstanceDefinitions, out block, out found);

      if (null != found)
        CommandArgs.FromInstanceDefinition(found);
      else if (ExistingInstanceDefinitions.Length > 0)
        CommandArgs.FromInstanceDefinition(ExistingInstanceDefinitions[0]);

      // If the block name property is empty then cook up a new, unused name
      if (string.IsNullOrWhiteSpace(block) && null != CommandArgs.Document)
        block = CommandArgs.Document.InstanceDefinitions.GetUnusedInstanceDefinitionName();

      blockName = block;
      CommandArgs.BlockId = Guid.Empty;
      CommandArgs.InsertSourceArchive = true;
      sourceArchive = fileName;

      CreatePreviewImage();
    }

    #region InsertBaseViewModel abstract methods
    protected override void CreatePreviewImage()
    {
#if ON_OS_WINDOWS
      PreviewBitmap = CommandArgs.GetPreviewBitmap(PreviewProjection, PreviewDisplayMode, new System.Drawing.Size(200, 120));
#endif
    }
    #endregion InsertBaseViewModel abstract methods

#if ON_OS_MAC
    public override bool WindowShouldClose ()
    {
      if (null == Window || Window.DialogResult != true)
        return true;
      return OkayToClose ();
    }
#endif

    #region Private methods
    /// <summary>
    /// Display "Invalid Insert Value" message box and set focus to specified control if passed
    /// </summary>
    /// <param name="message"></param>
    /// <param name="ctrl"></param>
    void InvalidValueMessageBox(string message)
    {
    #if ON_OS_MAC
      var messageBox = new MonoMac.AppKit.NSAlert()
      {
        MessageText = Rhino.UI.LOC.STR("Invalid Insert Value"),
        InformativeText = message,
        AlertStyle = MonoMac.AppKit.NSAlertStyle.Informational
      };
      messageBox.AddButton ("OK");
      messageBox.RunModal();
      messageBox.Dispose();
    #endif
    #if ON_OS_WINDOWS
      System.Windows.MessageBox.Show(Window,
                                     message,
                                     Rhino.UI.LOC.STR("Invalid Insert Value"),
                                     System.Windows.MessageBoxButton.OK,
                                     System.Windows.MessageBoxImage.Exclamation);
    #endif
    }
    internal bool OkayToClose()
    {
      //
      // Make sure the block name has a value
      //
      if (string.IsNullOrWhiteSpace(blockName))
      {
        InvalidValueMessageBox(Rhino.UI.LOC.STR("Please enter a block name to insert"));
        return false;
      }
      CommandArgs.BlockName = blockName;

      if (ExistingInstanceDefinitions.Length > 0 && CommandArgs.UpdateType == InstanceDefinitionUpdateType.Linked || CommandArgs.UpdateType == InstanceDefinitionUpdateType.LinkedAndEmbedded)
      {
        // Linked or Linked and embedded instance definition and there is currently one more instance definitions that reference this archive
        foreach (var def in ExistingInstanceDefinitions)
        {
          if (CommandArgs.FileNameOptionsMatch(def))
          {
            // This definition has the exact same parameters as the new one so just use it
            CommandArgs.FromInstanceDefinition(def);
            // Done here so bail
            return true;
          }
        }
      }

      CommandArgs.NeedToDisplayOptionsForm = false;

      //
      // Check to see if there is currently a block with this name
      //
      InstanceDefinition found = CommandArgs.FindInstanceDefintion(CommandArgs.BlockName);
      if (found != null)
      {
        // Display overwrite warning message box
        bool? dialogResult = null;
        var msg = string.Format(Rhino.UI.LOC.STR("The \"{0}\" block definition already exists. Do you want to replace it?"), CommandArgs.BlockName);
        var rc = 2;
        #if ON_OS_WINDOWS
        var result = System.Windows.MessageBox.Show(Window,
                                                    msg,
                                                    Rhino.UI.LOC.STR("Redefine Block"),
                                                    System.Windows.MessageBoxButton.YesNoCancel,
                                                    System.Windows.MessageBoxImage.Question);
        // Move this above the MessageBox.Show when it is hooked up on the Mac,
        // leving the above code to make sure it gets replaced with Mac code
        // before continuing.
        if (result == System.Windows.MessageBoxResult.Yes)
          rc = 0;
        else if (result == System.Windows.MessageBoxResult.No)
          rc = 1;
        else
          rc = 2;
        #endif
        #if ON_OS_MAC
        var messageBox = new MonoMac.AppKit.NSAlert()
        {
          MessageText = Rhino.UI.LOC.STR("Overwrite Block Definiton"),
          InformativeText = msg,
          AlertStyle = MonoMac.AppKit.NSAlertStyle.Informational
        };
        messageBox.AddButton("Yes");
        messageBox.AddButton("No");
        messageBox.AddButton("Cancel");
        var result = messageBox.RunModal();
        messageBox.Dispose();
        if (result == (long)MonoMac.AppKit.NSAlertButtonReturn.First)
          rc = 0; // yes
        else if (result == (long)MonoMac.AppKit.NSAlertButtonReturn.Second)
          rc = 1; // no
        else
          rc = 2; // cancel
        #endif
        switch (rc)
        {
          case 1: // No
            dialogResult = false;
            break;
          case 0: // Yes
            dialogResult = true;
            break;
          default: // 2 Cancel
            dialogResult = null;
            break;
        }
        if (dialogResult == false)
          return false; // Cancel the closing operation and return to the options dialog
        else if (dialogResult == true)
          CommandArgs.BlockId = found.Id; // Set the block ID to the block being overwritten
        else
          Window.DialogResult = false; // Cancel and close the options dialog
        Window.DialogResult = dialogResult;
        return true;
      }
      return true;
    }
    #endregion Private methods

    #region Public properties
    #if ON_OS_WINDOWS
    public System.Windows.Window Window { get; set; }
    #endif
    public string blockName
    {
      get { return CommandArgs.BlockName; }
      set
      {
        if (CommandArgs.BlockName == value) return;
        CommandArgs.BlockName = value;
        RaiseInvalidPropertyValue(() => blockName);
      }
    }
    public string sourceArchive
    {
      get { return CommandArgs.SourceArchive; }
      set
      {
        if (CommandArgs.SourceArchive == value) return;
        CommandArgs.SourceArchive = value;
        RaiseInvalidPropertyValue(() => sourceArchive);
      }
    }
    public bool sourceArchiveVisible
    {
      get { return _sourceArchiveVisible; }
      set
      {
        if (SetProperty(() => sourceArchiveVisible, value, ref _sourceArchiveVisible))
          RaisePropertyChanged(() => SourceArchiveHeightString);
      }
    }
    public string SourceArchiveHeightString
    {
      get { return (sourceArchiveVisible ? "Auto" : "0"); }
    }
    public bool readLinkedBlocks
    {
      get { return !CommandArgs.SkipNestedLinkedDefinitions; }
      set
      {
        bool b = !value;
        if (b == CommandArgs.SkipNestedLinkedDefinitions) return;
        CommandArgs.SkipNestedLinkedDefinitions = b;
        RaiseInvalidPropertyValue(() => readLinkedBlocks);
      }
    }
    public InstanceDefinitionUpdateType updateType
    {
      get { return CommandArgs.UpdateType; }
      set
      {
        if (value == CommandArgs.UpdateType) return;
        CommandArgs.UpdateType = value;
        RaisePropertyChanged(() => updateType);
        RaisePropertyChanged(() => updateTypeIndex);
      }
    }
    public int updateTypeIndex
    {
      get
      {
        switch (updateType)
        {
          case InstanceDefinitionUpdateType.Static:
          case InstanceDefinitionUpdateType.Embedded:
            return 0;
          case InstanceDefinitionUpdateType.LinkedAndEmbedded:
            return 1;
          case InstanceDefinitionUpdateType.Linked:
            return 2;
        }
        return -1;
      }
      set
      {
        if (value == 0)
          updateType = InstanceDefinitionUpdateType.Static;
        else if (value == 1)
          updateType = InstanceDefinitionUpdateType.LinkedAndEmbedded;
        else if (value == 2)
          updateType = InstanceDefinitionUpdateType.Linked;
      }
    }
    public InstanceDefinitionLayerStyle layerStyle
    {
      get { return CommandArgs.LayerStyle; }
      set
      {
        if (value == CommandArgs.LayerStyle) return;
        CommandArgs.LayerStyle = value;
        RaisePropertyChanged(() => layerStyle);
        RaisePropertyChanged(() => layerStyleIndex);
      }
    }
    public int layerStyleIndex
    {
      get
      {
        if (layerStyle == InstanceDefinitionLayerStyle.Reference)
          return 1;
        return 0;
      }
      set
      {
        if (value == 1)
          layerStyle = InstanceDefinitionLayerStyle.Reference;
        else
          layerStyle = InstanceDefinitionLayerStyle.Active;
      }
    }
    public string blockDescription
    {
      get { return CommandArgs.BlockDescription; }
      set
      {
        if (CommandArgs.BlockDescription == value) return;
        CommandArgs.BlockDescription = value;
        RaisePropertyChanged(() => blockDescription);
      }
    }
    public string url
    {
      get { return CommandArgs.Url; }
      set
      {
        if (CommandArgs.Url == value) return;
        CommandArgs.Url = value;
        RaisePropertyChanged(() => url);
      }
    }
    public string urlDescription
    {
      get { return CommandArgs.UrlDescription; }
      set
      {
        if (CommandArgs.UrlDescription == value) return;
        CommandArgs.UrlDescription = value;
        RaisePropertyChanged(() => urlDescription);
      }
    }
    public readonly InsertInstanceCommandOptions CommandArgs;
    #endregion Public properties

    #region private members
    private bool _sourceArchiveVisible = true;
    private readonly InstanceDefinition[] ExistingInstanceDefinitions;
    #endregion private members
  }
}
