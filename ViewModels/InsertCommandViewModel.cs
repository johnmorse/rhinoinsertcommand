using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;
using Rhino.Display;

namespace Insert.ViewModels
{
  //-------------------------------------------------------------------------
  /// <summary>
  /// Simple view model for creating a Rhino point object
  /// </summary>
  class InsertCommandViewModel : InsertBaseViewModel
  {
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="doc">Document used to create the new point</param>
    public InsertCommandViewModel(RhinoDoc doc, InsertInstanceCommandOptions initialCommandOptions, bool droppingFileOnRhino)
    : base(doc)
    {
      _doc = doc;
      _droppingFileOnRhino = droppingFileOnRhino;
      CommandOptions = initialCommandOptions;
      CommandOptions.DocumentId = doc.DocumentId;
      promptInsertionPoint = CommandOptions.PromptForInsertionPoint;
      InsertionPoint = CommandOptions.InsertionPoint;
      promptScale = CommandOptions.PromptForScale;
      uniformlyScale = CommandOptions.UniformlyScale;
      Scale = CommandOptions.Scale;
      promptRotation = CommandOptions.PromptForRotationAngle;
      rotation = RhinoMath.ToDegrees(CommandOptions.RotationAngle);
      //
      // Initialize list of hidden instance definitions
      //
      // The combo box is disabled when DroppingFileOnRhino is true so hidden InstanceDefinition objects will
      // never get added so don't bother building the list
      if (!DroppingFileOnRhino)
        foreach (var idef in Document.InstanceDefinitions)
          if (IsHiddenIDef(idef))
            HiddenIDefs.Add(new InsertInstanceOptions(idef, doc.DocumentId));
      //
      // Initialize bock name combo box
      //
      // IncludeIDefInList checks this flag so set it before initializing the combo box
      if (DroppingFileOnRhino)
      {
        // TODO: 
        //InsertCommandArgs item = new InsertCommandArgs(InsertCommandArgs);
        //item.DroppingFileOnRhino = true;
        //comboBoxName.Items.Add(item);
        //comboBoxName.SelectedIndex = 0;
        //buttonFile.Enabled = false;
      }
      else if (null != Document)
      {
        // Process the active documents InstanceDefinitions table and create a list of items to add
        var blocks = new List<InsertInstanceOptions>();
        foreach (var idef in Document.InstanceDefinitions)
          if (IncludeIDefInList(idef))
            blocks.Add(new InsertInstanceOptions(idef, Document.DocumentId));
        // Sort the list
        blocks.Sort(InsertInstanceOptions.Compare);
        int foundBlockName = -1;
        // Copy the list to the view model observable collection for binding by WPF or XCode
        foreach (var block in blocks)
        {
          blockList.Add(block);
          if (block.BlockId == BlockId)
            blockListSelectedIndex = blockList.Count - 1;
          if (foundBlockName < 0 && string.IsNullOrWhiteSpace(BlockName) && null != Document)
          {
            if (block.BlockName.Equals(BlockName, StringComparison.OrdinalIgnoreCase))
              foundBlockName = blockList.Count - 1;
          }
        }
        // TRR 21538: If no previous name exists, use the first active one on the list.
        if (blockListSelectedIndex < 0 && blockList.Count > 0)
          blockListSelectedIndex = Math.Max(0, foundBlockName);
      }
    #if ON_OS_WINDOWS
      // Register command callbacks for button click (command) events
      BrowseForFileButtonClickDelegate = new RhinoWindows.Input.DelegateCommand(WinBrowseForFileButtonClick, null);
    #endif
    }

    protected override void CreatePreviewImage()
    {
      SetPreviewImage(SelectedBlock);
    }

    #region Windows specific ICommand's
    #if ON_OS_WINDOWS
    public Insert.Win.InsertCommandWindow Window { get; set; }
    public System.Windows.Input.ICommand BrowseForFileButtonClickDelegate { get; private set; }
    public void WinBrowseForFileButtonClick()
    {
      var parent = RhinoWindows.Forms.WindowsInterop.ObjectAsIWin32Window(Window);
      // Display the Rhino File Import dialog and select file name to use as SourceArchive
      string fileName = Rhino.Input.RhinoGet.GetFileName(Rhino.Input.Custom.GetFileNameMode.Import,
                                                         string.Empty,
                                                         Rhino.UI.LOC.STR("Select File to Insert"),
                                                         parent);
      OnFileButtonClicked(fileName);    
    }
    #endif
    #endregion Windows specific ICommand's
    
    #region Mac specific commands
    #if ON_OS_MAC
    public void MacBrowseForFileButtonClick()
    {
      using (var panel = new MonoMac.AppKit.NSOpenPanel())
      {
        panel.CanChooseFiles = true;
        panel.CanChooseDirectories = false;
        var rc = panel.RunModal ();
        if (rc < 1)
          return;
        var urls = panel.Urls;
        if (null == urls || urls.Length < 1)
          return;
        var fileName = urls [0].Path;
        OnFileButtonClicked (fileName);
      }
    }
    public override bool WindowShouldClose ()
    {
      if (null == Window || Window.DialogResult != true)
        return true;
      return OkayToClose ();
    }
    #endif
    #endregion

    #region Button click commands
    private void OnFileButtonClicked(string fileName)
    {
      // File dialog canceled
      if (string.IsNullOrWhiteSpace(fileName))
        return;
      if (!File.Exists(fileName))
      {
        InvalidValueMessageBox(string.Format(Rhino.UI.LOC.STR("File \"{0}\" not found"), fileName));
        return;
      }
      // Make sure you are not attempting to insert this model into itself
      if (InsertAs == ViewModels.InsertAs.Block && string.Compare(ModelPath, fileName, StringComparison.InvariantCultureIgnoreCase) == 0)
      {
        InvalidValueMessageBox(Rhino.UI.LOC.STR("You can not insert a model into itself"));
        return;
      }
      // Not inserting as a block so just cook up a name from the file name and add it to the list
      var item = new InsertInstanceOptions(CommandOptions);
      // Default block name is file name without extension
      var blockName = Path.GetFileNameWithoutExtension(fileName);
      // If there is already a block with this name cook up a decorated name
      if (null != Document && null != Document.InstanceDefinitions.Find(blockName, true))
        blockName = Document.InstanceDefinitions.GetUnusedInstanceDefinitionName(blockName);
      // Default insertion command arguments
      var defaults = InsertInstanceOptions.Defaults;
      // Set the new items properties using the default insert values
      item.FromFileName(fileName, blockName, defaults.UpdateType, defaults.LayerStyle, defaults.SkipNestedLinkedDefinitions);
      // Set the flag that will require the options form to be displayed when this dialog is closing if this item is selected
      item.NeedToDisplayOptionsForm = true;
      // Add the new item to the combo box
      _blockList.Add(item);
      RaisePropertyChanged(() => blockList);
      RaisePropertyChanged(() => blockStringList);
      // Select the newly added item
      blockListSelectedIndex = _blockList.Count - 1;
    }
    #endregion Button click commands

    #region Public methods
    public bool OkayToClose()
    {
      var selected = SelectedBlock;
      if (UpdateCommandArgs(false, ref selected))
      {
        CommandOptions.CopyFrom(selected);
        return true;
      }
      return false;
    }
    #endregion Plublic methods

    #region Private methods
    /// <summary>
    /// Update CommandArgs
    /// </summary>
    /// <param name="failSilently"></param>
    /// <param name="instanceOptions"></param>
    /// <returns>Returns true if everything is okay or false if some condition existed that required the user to confirm some
    /// value prior to closing and they chose not to</returns>
    bool UpdateCommandArgs(bool failSilently, ref InsertInstanceOptions instanceOptions)
    {
      if (null == instanceOptions)
      {
        if (!failSilently)
          InvalidValueMessageBox(Rhino.UI.LOC.STR("You must select a block or file to insert"));
        return false;
      }
      // If inserting as a block and it is a new block which is going to be created from an external file and the InsertFileOptionsForm
      // has never been displayed for this item then display the form
      if (InsertAs == InsertAs.Block && instanceOptions.InsertSourceArchive && instanceOptions.NeedToDisplayOptionsForm)
      {
        var temp = ShowOptionsForm(instanceOptions.SourceArchive, instanceOptions.BlockDescription, instanceOptions.UrlDescription, instanceOptions.Url, true);
        if (null == temp)
          return false;
        // Copy the settings from the options form to the specified item
        instanceOptions.CopyFrom(temp);
        instanceOptions.DocumentId = DocumentId;
      }
      // Set the insert as value
      CommandOptions.InsertAs = InsertAs;
      // Update the insertion point values based on the current dialog settings
      CommandOptions.PromptForInsertionPoint = promptInsertionPoint;
      CommandOptions.InsertionPoint = InsertionPoint;
      // Update the scale setting flags based on the current dialog values
      CommandOptions.PromptForScale = promptScale;
      CommandOptions.UniformlyScale = uniformlyScale;
      // Update rotation settings based on current dialog values
      CommandOptions.PromptForRotationAngle = promptRotation;
      CommandOptions.RotationAngle = RhinoMath.ToRadians(rotation);
      //
      // Validate scale, must be non-zero value
      //
      // Will only validate controls that are enabled
      var xScale = enableScaleXControls ? scaleX : 1.0;
      var yScale = enableScaleYZControls ? scaleY : 1.0;
      var zScale = enableScaleYZControls ? scaleZ : 1.0;
      if (uniformlyScale)
        CommandOptions.Scale = new Point3d(xScale, xScale, xScale);
      else
        CommandOptions.Scale = new Point3d(xScale, yScale, zScale);
      // Make sure you are not attempting to insert this model into itself
      if (InsertAs == InsertAs.Block && instanceOptions.InsertSourceArchive && 0 == string.Compare(instanceOptions.SourceArchive, DocumentPath, StringComparison.InvariantCultureIgnoreCase))
      {
        InvalidValueMessageBox(Rhino.UI.LOC.STR("You can not insert a model into itself"));
        return false;
      }

      return true;
    }
    InsertInstanceOptions ShowOptionsForm(string fileName, string description, string urlDescription, string url, bool hideThisForm)
    {
      // Inserting as a block so display the block options form
      var args = new InsertInstanceCommandOptions(CommandOptions);
      args.BlockDescription = description;
      args.UrlDescription = urlDescription;
      args.Url = url;
      var model = new BlockPropertiesViewModel(Document, fileName, args);
      model.sourceArchiveVisible = false;
      bool? dialogResult = null;
    #if ON_OS_WINDOWS
      model.Window = new Win.BlockPropertiesWindow();
      model.Window.DataContext = model;
      model.Window.Owner = Window;
      // TODO:
      //if (hideThisForm)
      //{
      //  model.Window.ContentRendered += Window_ContentRendered;
      //  model.Window.Closing += Window_Closed;
      //  Window.Closing += Window_Closing;
      //}
      model.Window.ShowDialog();
      //if (hideThisForm)
      //{
      //  model.Window.ContentRendered -= Window_ContentRendered;
      //  model.Window.Closed -= Window_Closed;
      //}
      dialogResult = model.Window.DialogResult;
    #endif
    #if ON_OS_MAC
      // Create a NSWindow from a Nib file
      var window = RhinoMac.Window.FromNib("InsertFileOptionsWindow", model);
      // Associate the window with the View Model so that the
      // model's Okay and Cancel methods can close the window
      model.Window = window;
      // Display the window
      window.ShowModal();
      // Success will be true if the window was closed by the
      // OK button otherwise it should be false.
      dialogResult = window.DialogResult;
    #endif
      if (dialogResult != true)
        return null;
      InsertInstanceOptions result = null;
      // This use to be connected to a Win Form but it is now connected to a
      // model view controller which gets modified by either a WPF Window or
      // a XCode Window
      if (model.CommandArgs.InsertSourceArchive && Guid.Empty == model.CommandArgs.BlockId)
      {
        // A brand new block is being created so add the new block to the list
        _blockList.Add(new InsertInstanceOptions(model.CommandArgs));
        blockListSelectedIndex = (_blockList.Count - 1);
        result = SelectedBlock;
      }
      else
      {
        // Find existing item and make it match what was returned
        int i = IndexFromBlockId(model.CommandArgs.BlockId);
        if (i >= 0)
        {
          if (model.CommandArgs.InsertSourceArchive)
          {
            var item = _blockList[i];
            if (null != item)
            {
              // Copy data from the new item to the existing combo box item, this will clear
              // the preview image
              item.CopyFrom(model.CommandArgs);
            }
          }
          // Set the combo box selection to the item being overridden
          blockListSelectedIndex = i;
          result = SelectedBlock;
        }
      }
      return result;
    }
    /// <summary>
    /// Iterate the name combo box and return the index of the first instance found with the specified Id
    /// </summary>
    /// <param name="id"></param>
    /// <returns>Index of item if found in the list otherwise; -1 is returned</returns>
    int IndexFromBlockId(Guid id)
    {
      if (Guid.Empty != id)
        for (int i = 0; i < _blockList.Count; i++)
        {
          var item = _blockList[i];
          if (null != item && item.BlockId == id)
            return i;
        }
      return -1;
    }

    #if ON_OS_WINDOWS
    void Window_Closed(object sender, EventArgs e)
    {
      // Doing this causes the Window to become visible but it is no longer modal and
      // the original ShowDialog() call will return ending the command.
      var optionsWindow = sender as Win.BlockPropertiesWindow;
      if (!Window.IsVisible && null != optionsWindow && optionsWindow.DialogResult != true)
        Window.Visibility = System.Windows.Visibility.Visible;
    }

    void Window_ContentRendered(object sender, EventArgs e)
    {
      var optionsWindow = sender as Win.BlockPropertiesWindow;
      if (null != optionsWindow && optionsWindow.IsVisible && Window.IsVisible)
        Window.Visibility = System.Windows.Visibility.Collapsed;
    }
    #endif

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
    /// <summary>
    /// Initialize the combo box text field and description label text based on the current InstanceDefinition
    /// </summary>
    /// <param name="item"></param>
    void InitializeFromInsertInstance(InsertInstanceOptions item)
    {
      url = Rhino.UI.LOC.STR("URL: ");
      int start = url.Length;
      int end = start;

      InstanceDefinition idef = null == item ? null : item.InstanceDefinition;

      if (item == null)
      {
        linkDescription = string.Empty;
        blockDescription = string.Empty;
      }
      else if (null != idef)
      {
        url += string.IsNullOrWhiteSpace(idef.UrlDescription) ? idef.Url : idef.UrlDescription;
        blockDescription = idef.Description;
        end = url.Length;
      }
      else
      {
        blockDescription = item.BlockDescription;
      }

      // TODO:
      // linkLabel.LinkArea = new LinkArea(start, end - start);

      UpdateDescriptionText(item);

      SetPreviewImage(item);
    }
    /// <summary>
    /// Initialize the block preview image control
    /// </summary>
    /// <param name="item"></param>
    /// <summary>
    /// Initialize the block preview image control
    /// </summary>
    /// <param name="item"></param>
    void SetPreviewImage(InsertInstanceOptions item)
    {
    #if ON_OS_WINDOWS
      if (null == item)
      {
        PreviewBitmap = null;
        return;
      }
      var cursor = null == Window ? null : Window.Cursor;
      if (null != Window) Window.Cursor = System.Windows.Input.Cursors.Wait;
      PreviewBitmap = item.GetPreviewBitmap(PreviewProjection, PreviewDisplayMode, new System.Drawing.Size(200, 120));
      if (null != Window) Window.Cursor = cursor;
    #endif
    }
    /// <summary>
    /// Update the description text label based on current CommandArgs values
    /// </summary>
    void UpdateDescriptionText(InsertInstanceOptions instance)
    {
      blockDescriptionIsReadOnly = (InsertAs == InsertAs.Block || null == instance || !instance.NeedToDisplayOptionsForm);
      // Catch formatting exceptions on Asian language systems.
      try
      {
        if (DroppingFileOnRhino)
        {
          linkDescription = string.Format(Rhino.UI.LOC.STR("Dropping file \"{0}\" onto Rhino"), FormatPathForDisplay(CommandOptions));
        }
        else if (null == instance || string.IsNullOrWhiteSpace(instance.BlockName))
        {
          linkDescription = string.Empty;
        }
        else
        {
          // Update the description text to include the link style and SourceArchive if included
          if (instance.NeedToDisplayOptionsForm && InsertAs == InsertAs.Block)
            linkDescription = string.Format(Rhino.UI.LOC.STR("Creating block from file \"{0}\""), FormatPathForDisplay(instance));
          else if (instance.NeedToDisplayOptionsForm)
            linkDescription = string.Format(Rhino.UI.LOC.STR("Inserting file \"{0}\""), FormatPathForDisplay(instance));
          else if (instance.UpdateType == InstanceDefinitionUpdateType.Linked)
            linkDescription = string.Format(Rhino.UI.LOC.STR("Linked to file \"{0}\""), FormatPathForDisplay(instance));
          else if (instance.UpdateType == InstanceDefinitionUpdateType.LinkedAndEmbedded)
            linkDescription = string.Format(Rhino.UI.LOC.STR("Embedded and linked to file \"{0}\""), FormatPathForDisplay(instance));
          else
            linkDescription = Rhino.UI.LOC.STR("Embedded block");
        }
      }
      catch (Exception exception)
      {
        linkDescription = string.Empty;
        Rhino.Runtime.HostUtils.ExceptionReport(exception);
      }
    }
    string FormatPathForDisplay(InsertInstanceOptions instance)
    {
      if (null == instance)
        return string.Empty;

      string path = instance.SourceArchive;

      if (string.IsNullOrWhiteSpace(path))
        return string.Empty;

      string result = path;

      if (!string.IsNullOrWhiteSpace(ModelPath) && Path.IsPathRooted(path))
      { // If using absolute paths and active model is named and path is a relative path
        result = PathAbsoluteFromRelativeTo(path, true, ModelPath, true);
      }

      return (result ?? string.Empty);
    }
    /// <summary>
    /// I was going to port this from Rhino but decided I wanted to make sure it did what the rest of the
    /// built in Rhino insert does and did not want to have to keep it up to date in Rhino and here
    /// </summary>
    /// <param name="relativePath"></param>
    /// <param name="bRelativePathisFileName"></param>
    /// <param name="relativeTo"></param>
    /// <param name="bRelativeToIsFileName"></param>
    /// <returns></returns>
    string PathAbsoluteFromRelativeTo(string relativePath, bool bRelativePathisFileName, string relativeTo, bool bRelativeToIsFileName)
    {
      // TODO: return RMA.UI.RuiManager.InsertCmdPathAbsoluteFromRelativeTo(relativePath, bRelativePathisFileName, relativeTo, bRelativeToIsFileName);
      return Path.GetFullPath(relativePath);
    }
    /// <summary>
    /// The InstanceDefinition is considered a "hidden" or "anonymous" InstanceDefinition if the name starts with '*'
    /// </summary>
    /// <param name="idef"></param>
    /// <returns>Returns true if the specified InstanceDefinition is non null and is a hidden InstanceDefinition</returns>
    bool IsHiddenIDef(InstanceDefinition idef)
    {
      // The only current check is if the first char is a '*' then the InstanceDefinition is considered
      // a hidden or anonymous InstanceDefinition
      // July 10, 2012 Tim/JohnM, Potential fix for RR 108618.  Check to make sure Name is good before using it too.
      return (null != idef && !string.IsNullOrEmpty(idef.Name) && idef.Name.StartsWith("*"));
    }
    /// <summary>
    /// Call this method to decide if a InstanceDefinition should be added to the combo box
    /// </summary>
    /// <param name="idef"></param>
    /// <returns></returns>
    bool IncludeIDefInList(InstanceDefinition idef)
    {
      // Ignore delete, tenuous (defined in linked files) and referenced InstanceDefintion objects
      // 20 August 2012 John Morse http://dev.mcneel.com/bugtrack/?q=112140
      // Updating a referenced file that includes blocks causes the documents block table to contain
      // null Instance Definition pointers, when this happens the Id will be Guid.Empty and the
      // Instance Definition Name property will return NULL which causes problems when adding
      // the item to a ComboBox so I added the idef.Id == Guid.Empty test to filter out null definitions.
      if (null == idef || idef.IsDeleted || idef.Id == Guid.Empty || null == idef.Name || idef.IsTenuous || idef.IsReference)
        return false;
      // Pay attention to the hidden block display flag
      if (!ShowHiddenBlockDefinitions && IsHiddenIDef(idef))
        return false;
      return true;
    }
    #endregion Private methods

    #region Public properties
    /// <summary>
    /// Collection of blocks currently in the model, will include new blocks
    /// created by clicking the file button in the UI.
    /// </summary>
    public ObservableCollection<InsertInstanceOptions> blockList
    {
      get { return _blockList; }
    }
    /// <summary>
    /// Gets a list of block name strings, need this for the Mac so it can
    /// get converted to a NSArray.
    /// </summary>
    /// <value>The block string list.</value>
    public List<string> blockStringList
    {
      get
      {
        var result = new List<string>(blockList.Count);
        foreach (var block in blockList)
          result.Add(block.ToString());
        return result;
      }
    }
    /// <summary>
    /// Could not figure out how to bind selected index on the mac to a combo
    /// box whoes content is a list of strings, it seems to want the string
    /// that is in the combo box so this looks up string and sets/gets the
    /// index accordingly.
    /// </summary>
    /// <value>The name of the selected block.</value>
    public string blockListSelectedName
    {
      get
      {
        if (blockListSelectedIndex < 0 || blockListSelectedIndex >= _blockList.Count)
          return string.Empty;
        return _blockList[blockListSelectedIndex].ToString();
      }
      set
      {
        var index = -1;
        var found = 0;
        if (!string.IsNullOrWhiteSpace(value))
        {
          foreach (var block in blockList)
          {
            if (value == block.ToString())
            {
              index = found;
              break;
            }
            else
              found++;
          }
        }
        blockListSelectedIndex = index;
      }
    }
    /// <summary>
    /// Selected instance index
    /// </summary>
    public int blockListSelectedIndex
    {
      get { return _blockListSelectedIndex; }
      set
      {
        if (SetProperty(() => blockListSelectedIndex, value, ref _blockListSelectedIndex))
        {
          InitializeFromInsertInstance(SelectedBlock);
          RaisePropertyChanged(() => blockListSelectedName);
        }
      }
    }
    /// <summary>
    /// The currently selected instance
    /// </summary>
    public InsertInstanceOptions SelectedBlock
    {
      get
      {
        if (blockListSelectedIndex < 0 || blockListSelectedIndex >= blockList.Count)
          return null;
        return blockList[blockListSelectedIndex];
      }
    }
    /// <summary>
    /// Extract the full path to the active model
    /// </summary>
    public string DocumentPath
    {
      get { return (null == Document ? String.Empty : Document.Path); }
    }
    /// <summary>
    /// Flag which specifies whether hidden InstanceDefinition objects should
    /// be included in the combo box
    /// </summary>
    public bool ShowHiddenBlockDefinitions
    {
      get { return _showHiddenBlockDefinitions; }
      set
      {
        SetProperty(() => ShowHiddenBlockDefinitions, value, ref _showHiddenBlockDefinitions);
      }
    }
    /// <summary>
    /// Look up BlockId in Document and return the associated InstanceDefinition
    /// </summary>
    public InstanceDefinition InstanceDefinition
    {
      get
      {
        RhinoDoc doc = Document;
        if (null != doc)
          return doc.InstanceDefinitions.Find(BlockId, true);
        return null;
      }
    }
    /// <summary>
    /// Will be true if the options form has never been displayed for a new item with InsertSourceArchive set to true
    /// </summary>
    public bool NeedToDisplayOptionsForm
    {
      get { return _needToDisplayOptionsForm; }
      set { SetProperty(() => NeedToDisplayOptionsForm, value, ref _needToDisplayOptionsForm); }
    }
    /// <summary>
    /// The serial number for the document get block list from
    /// </summary>
    public int DocumentId { get; set; }
    /// <summary>
    /// Name of the block to insert, this will come from the InstanceDefinition associated with BlockID
    /// unless InsertSourceArchive it true in which case a new block will get created using BlockName
    /// </summary>
    public string BlockName
    {
      get { return _blockName; }
      set { SetProperty(() => BlockName, value, ref _blockName); }
    }
    /// <summary>
    /// Static text that describes the link style when inserting as a block.
    /// </summary>
    public string linkDescription
    {
      get { return _linkDescription; }
      set { SetProperty(() => linkDescription, value, ref _linkDescription); }
    }
    /// <summary>
    /// Block description, only used when creating a new block or when redefining an existing one.
    /// </summary>
    public string blockDescription
    {
      get { return _blockDescription; }
      set { SetProperty(() => blockDescription, value, ref _blockDescription); }
    }
    /// <summary>
    /// Should only be enabled when creating a new instance defintion from a
    /// file.
    /// </summary>
    public bool blockDescriptionIsReadOnly
    {
      get { return _blockDescriptionIsReadOnly; }
      set
      {
        if (SetProperty (() => blockDescriptionIsReadOnly, value, ref _blockDescriptionIsReadOnly))
          RaisePropertyChanged (() => blockDescriptionIsEditable);
      }
    }
    /// <summary>
    /// Should only be enabled when creating a new instance defintion from a
    /// file.
    /// </summary>
    public bool blockDescriptionIsEditable
    {
      get { return (!blockDescriptionIsReadOnly); }
      set { blockDescriptionIsReadOnly = !value; }
    }
    /// <summary>
    /// Url descriptive string which is displayed as a hyperlink by the user interface, only used when
    /// creating a new block or when redefining an existing one.
    /// </summary>
    public string url
    {
      get { return _url; }
      set { SetProperty(() => url, value, ref _url); }
    }
    /// <summary>
    /// Block URL string, only used when creating a new block or when redefining an existing block
    /// </summary>
    public string UrlDescription
    {
      get { return _urlDescription; }
      set { SetProperty(() => UrlDescription, value, ref _urlDescription); }
    }
    /// <summary>
    /// ID of the block to insert
    /// </summary>
    public Guid BlockId { get; set; }
    /// <summary>
    /// If InsertSourceArchive is true then create a new block or update and existing block using this file
    /// </summary>
    public string SourceArchive
    {
      get { return _sourceArchive; }
      set { SetProperty(() => SourceArchive, value, ref _sourceArchive); }
    }
    /// <summary>
    /// If this is true and SourceArchive is not empty then do one of the following:
    /// 1) If BlockID is empty then create a new InstanceDefnition using the UpdateType, LayerStyle, and
    ///    ReadLinkedBlocks flags  and BlockName
    /// 2) If BlockID points to an existing InstanceDefnition then update the existing definition using
    ///    the specified SourceArchive
    /// </summary>
    public bool InsertSourceArchive
    {
      get { return _insertSourceArchive; }
      set { SetProperty(() => InsertSourceArchive, value, ref _insertSourceArchive); }
    }
    /// <summary>
    /// <summary>
    /// The update depth is calculated from the UpdateType
    /// </summary>
    public bool SkipNestedLinkedDefinitions
    {
      get { return _skipNestedLinkedDefinitions; }
      set { SetProperty(() => SkipNestedLinkedDefinitions, value, ref _skipNestedLinkedDefinitions); }
    }
    /// <summary>
    /// Controls what is created by the Insert command
    /// </summary>
    public InsertAs InsertAs
    {
      get
      {
        return _insertAs;
      }
      set
      {
        if (value == _insertAs) return;
        _insertAs = value;
        RaisePropertyChanged(() => InsertAs);
        RaisePropertyChanged(() => insertAsIndex);
      }
    }
    /// <summary>
    /// Controls what is created by the Insert command
    /// </summary>
    public int insertAsIndex
    {
      get { return (int)_insertAs; }
      set
      {
        if (value >= 0 && value < 3) InsertAs = (InsertAs)value;
      }
    }
    /// <summary>
    /// Only valid when creating a new InstanceDefinition
    /// </summary>
    public InstanceDefinitionUpdateType UpdateType
    {
      get { return _updateType; }
      set
      {
        if (value == _updateType) return;
        _updateType = value;
        RaisePropertyChanged(() => UpdateType);
        RaisePropertyChanged(() => UpdateTypeIndex);
      }
    }
    /// <summary>
    /// Only valid when creating a new InstanceDefinition
    /// </summary>
    public int UpdateTypeIndex
    {
      get
      {
        switch (UpdateType)
        {
          case InstanceDefinitionUpdateType.Static:
          case InstanceDefinitionUpdateType.Embedded:
            return 0;
          case InstanceDefinitionUpdateType.Linked:
            return 1;
        }
        return 2;
      }
      set
      {
        if (value < 0 || value > 2 || value == UpdateTypeIndex) return;
        if (value == 0)
          UpdateType = InstanceDefinitionUpdateType.Static;
        else if (value == 1)
          UpdateType = InstanceDefinitionUpdateType.Linked;
        else
          UpdateType = InstanceDefinitionUpdateType.LinkedAndEmbedded;
      }
    }
    /// <summary>
    /// Only valid when creating a new InstanceDefinition
    /// </summary>
    public InstanceDefinitionLayerStyle LayerStyle
    {
      get { return _layerStyle; }
      set
      {
        if (value == _layerStyle) return;
        _layerStyle = value;
        RaisePropertyChanged(() => LayerStyle);
        RaisePropertyChanged(() => LayerStyleIndex);
      }
    }
    /// <summary>
    /// Only valid when creating a new InstanceDefinition
    /// </summary>
    public int LayerStyleIndex
    {
      get { return (int)(LayerStyle == InstanceDefinitionLayerStyle.None ? InstanceDefinitionLayerStyle.Active : LayerStyle); }
      set
      {
        if (value < 1 || value > 2) return;
        LayerStyle = (InstanceDefinitionLayerStyle)value;
      }
    }
    /// <summary>
    /// If this is true then the user is prompted for an insertion point when the
    /// form closes otherwise; InsertionPoint is used
    /// </summary>
    public bool promptInsertionPoint
    {
      get { return _promptInsertionPoint; }
      set
      {
        if (SetProperty(() => promptInsertionPoint, value, ref _promptInsertionPoint))
        {
          RaisePropertyChanged(() => InsertPointControlsVisibitliyString);
          RaisePropertyChanged(() => enableInsertionPointControls);
          RaisePropertyChanged(() => promptInsertionPointIndex);
        }
      }
    }
    /// <summary>
    /// Trying out using radio buttons instead of a check box on the mac so map
    /// the radio button index the correct checkbox state.
    /// </summary>
    /// <value>The index of the prompt insertion point.</value>
    public int promptInsertionPointIndex
    {
      get { return (promptInsertionPoint ? 0 : 1); }
      set { promptInsertionPoint = value == 0; }
    }
    /// <summary>
    /// Return WPF Visiblility property string to control showing/hiding
    /// insertion point controls.
    /// </summary>
    public string InsertPointControlsVisibitliyString { get { return (promptInsertionPoint ? "Hidden" : "Visible"); } }
    /// <summary>
    /// Enable insertion point input controls if the prompt check box is not
    /// currently checked.
    /// </summary>
    public bool enableInsertionPointControls { get { return !promptInsertionPoint; } }
    /// <summary>
    /// Only meaningful if PromptInsertionPoint is false, if it is then this point
    /// is used as the new blocks insertion point
    /// </summary>
    public double insertionPointX
    {
      get { return _insertionPoint.X; }
      set
      {
        if (double.IsNaN(value))
        {
          RaiseInvalidPropertyValue(() => insertionPointX);
          return;
        }
        if (_insertionPoint.X == value) return;
        _insertionPoint.X = value;
        RaisePropertyChanged(() => insertionPointX);
      }
    }
    /// <summary>
    /// Only meaningful if PromptInsertionPoint is false, if it is then this point
    /// is used as the new blocks insertion point
    /// </summary>
    public double insertionPointY
    {
      get { return _insertionPoint.Y; }
      set
      {
        if (double.IsNaN(value))
        {
          RaiseInvalidPropertyValue(() => insertionPointY);
          return;
        }
        if (_insertionPoint.Y == value) return;
        _insertionPoint.Y = value;
        RaisePropertyChanged(() => insertionPointY);
      }
    }
    /// <summary>
    /// Only meaningful if PromptInsertionPoint is false, if it is then this point
    /// is used as the new blocks insertion point
    /// </summary>
    public double insertionPointZ
    {
      get { return _insertionPoint.Z; }
      set
      {
        if (double.IsNaN(value))
        {
          RaiseInvalidPropertyValue(() => insertionPointZ);
          return;
        }
        if (_insertionPoint.Z == value) return;
        _insertionPoint.Z = value;
        RaisePropertyChanged(() => insertionPointZ);
      }
    }
    /// <summary>
    /// Only meaningful if PromptInsertionPoint is false, if it is then this point
    /// is used as the new blocks insertion point
    /// </summary>
    public Point3d InsertionPoint
    {
      get { return _insertionPoint; }
      set
      {
        if (!SetProperty(() => InsertionPoint, value, ref _insertionPoint)) return;
        RaisePropertyChanged(() => InsertionPoint);
        RaisePropertyChanged(() => insertionPointX);
        RaisePropertyChanged(() => insertionPointY);
        RaisePropertyChanged(() => insertionPointZ);
      }
    }
    /// <summary>
    /// If this is true then the user is prompted for insertion scale when the form
    /// is closed otherwise; Scale is used
    /// </summary>
    public bool promptScale
    {
      get { return _promptScale; }
      set
      {
        if (SetProperty(() => promptScale, value, ref _promptScale))
        {
          RaisePropertyChanged(() => ScaleXControlsVisibitliyString);
          RaisePropertyChanged(() => ScaleYZControlsVisibitliyString);
          RaisePropertyChanged(() => enableScaleXControls);
          RaisePropertyChanged(() => enableScaleYZControls);
          RaisePropertyChanged(() => promptScaleIndex);
          RaisePropertyChanged(() => enableUniformScaleEditControls);
        }
      }
    }
    /// <summary>
    /// Used by scle radio buttons on the Mac
    /// </summary>
    /// <value>The index of the prompt scale.</value>
    public int promptScaleIndex
    {
      get
      {
        if (promptScale) return 0;
        return (uniformlyScale ? 1 : 2);
      }
      set
      {
        if (value == 0)
          promptScale = true;
        else if (value == 1 || value == 2)
        {
          promptScale = false;
          uniformlyScale = (value == 1);
        }
      }
    }
    /// <summary>
    /// Return WPF Visiblility property string to control showing/hiding
    /// scale X and uniformly controls.
    /// </summary>
    public string ScaleXControlsVisibitliyString { get { return (promptScale ? "Hidden" : "Visible"); } }
    /// <summary>
    /// Return WPF Visiblility property string to control showing/hiding
    /// scale Y and Z controls.
    /// </summary>
    public string ScaleYZControlsVisibitliyString { get { return (promptScale || uniformlyScale ? "Hidden" : "Visible"); } }
    /// <summary>
    /// Enable scale X input controls if the prompt check box is not currently
    /// checked.
    /// </summary>
    public bool enableScaleXControls { get { return !promptScale; } }
    /// <summary>
    /// Enable scale Y and Z input controls if the prompt and uniformly scale
    /// check boxes are not currently checked.
    /// </summary>
    public bool enableScaleYZControls { get { return (!promptScale && !uniformlyScale); } }
    /// <summary>
    /// If prompt for scale is not enabled and uniform scale is enabled then 
    /// enable the uniform scale edit controls in the Mac UI
    /// </summary>
    /// <value><c>true</c> if enable uniform scale edit controls; otherwise, <c>false</c>.</value>
    public bool enableUniformScaleEditControls
    {
      get { return (!promptScale && uniformlyScale); }
    }
    /// <summary>
    /// Only meaningful if PromptScale is false, if so then the Scale.X is used
    /// for the new block inserts X, Y and Z scale.
    /// </summary>
    public bool uniformlyScale
    {
      get { return _uniformlyScale; }
      set
      {
        if (SetProperty(() => uniformlyScale, value, ref _uniformlyScale))
        {
          RaisePropertyChanged(() => ScaleYZControlsVisibitliyString);
          RaisePropertyChanged(() => enableScaleYZControls);
          RaisePropertyChanged(() => promptScaleIndex);
          RaisePropertyChanged(() => enableUniformScaleEditControls);
        }
      }
    }
    /// <summary>
    /// New block insertions Scale, this is only used if PromptScale is false
    /// </summary>
    public Point3d Scale
    {
      get { return _scale; }
      set
      {
        if (!SetProperty(() => Scale, value, ref _scale)) return;
        RaisePropertyChanged(() => scaleX);
        RaisePropertyChanged(() => scaleY);
        RaisePropertyChanged(() => scaleZ);
      }
    }
    /// <summary>
    /// New block insertions Scale, this is only used if PromptScale is false
    /// </summary>
    public double scaleX
    {
      get { return _scale.X; }
      set
      {
        if (value == 0.0)
        {
          RaiseInvalidPropertyValue(() => scaleX);
          return;
        }
        if (_scale.X == value) return;
        _scale.X = value;
        RaisePropertyChanged(() => scaleX);
      }
    }
    /// <summary>
    /// New block insertions Scale, this is only used if PromptScale is false
    /// </summary>
    public double scaleY
    {
      get { return _scale.Y; }
      set
      {
        if (value == 0.0)
        {
          RaiseInvalidPropertyValue(() => scaleX);
          return;
        }
        if (_scale.Y == value) return;
        _scale.Y = value;
        RaisePropertyChanged(() => scaleY);
      }
    }
    /// <summary>
    /// New block insertions Scale, this is only used if PromptScale is false
    /// </summary>
    public double scaleZ
    {
      get { return _scale.Z; }
      set
      {
        if (value == 0.0)
        {
          RaiseInvalidPropertyValue(() => scaleX);
          return;
        }
        if (_scale.Z == value) return;
        _scale.Z = value;
        RaisePropertyChanged(() => scaleZ);
      }
    }
    /// <summary>
    /// If this is true then the user is prompted for a rotation angle for the 
    /// new block insert when the form is closed otherwise; Rotation value is used
    /// </summary>
    public bool promptRotation
    {
      get { return _promptRotation; }
      set
      {
        if (SetProperty(() => promptRotation, value, ref _promptRotation))
        {
          RaisePropertyChanged(() => RotationControlsVisibitliyString);
          RaisePropertyChanged(() => enableRotationControls);
          RaisePropertyChanged(() => promptRotationIndex);
        }
      }
    }
    public int promptRotationIndex
    {
      get { return (promptRotation ? 0 : 1); }
      set { promptRotation = (value == 0); }
    }
    /// <summary>
    /// Return WPF Visiblility property string to control showing/hiding
    /// scale X and uniformly controls.
    /// </summary>
    public string RotationControlsVisibitliyString { get { return (promptRotation ? "Hidden" : "Visible"); } }
    /// <summary>
    /// Enable rotation angle input controls if the prompt check box is not
    /// currently checked.
    /// </summary>
    public bool enableRotationControls { get { return !promptRotation; } }
    /// <summary>
    /// Only valid if PromptRotation is false, if so then this value is used for
    /// the new block inserts rotation angle
    /// </summary>
    public double rotation
    {
      get { return _rotation; }
      set { SetProperty(() => rotation, value, ref _rotation); }
    }
    /// <summary>
    /// This will be true if this dialog was displayed as a result of dropping a file onto the Rhino main frame window
    /// and choosing insert.
    /// </summary>
    public bool DroppingFileOnRhino { get { return _droppingFileOnRhino; } }
    /// <summary>
    /// Originally passed InsertCommandArgs, these will get updated when the OK button is pressed if everything checks out
    /// </summary>
    readonly public InsertInstanceCommandOptions CommandOptions;
    /// <summary>
    /// Extract the full path to the active model
    /// </summary>
    string ModelPath { get { return DocumentPath; } }
    #endregion Public properties

    #region Private properties
    /// <summary>
    /// Sorted list of hidden blocks which is initialized when the form is loaded and is used by AddHiddenIDefs
    /// </summary>
    readonly List<InsertInstanceOptions> HiddenIDefs;
    #endregion Private properties

    #region Private members
    /// <summary>
    /// The document used to create the new point
    /// </summary>
    private readonly RhinoDoc _doc;
    private bool _needToDisplayOptionsForm;
    private string _blockName = string.Empty;
    private string _linkDescription = string.Empty;
    private string _blockDescription = string.Empty;
    private string _url = string.Empty;
    private string _urlDescription = string.Empty;
    private string _sourceArchive = string.Empty;
    private bool _insertSourceArchive;
    private readonly bool _droppingFileOnRhino;
    private bool _skipNestedLinkedDefinitions;
    private InsertAs _insertAs = InsertAs.Block;
    private InstanceDefinitionUpdateType _updateType = InstanceDefinitionUpdateType.Static;
    private InstanceDefinitionLayerStyle _layerStyle = InstanceDefinitionLayerStyle.Active;
    private bool _promptInsertionPoint = true;
    private Point3d _insertionPoint = Point3d.Origin;
    private bool _promptScale;
    private bool _uniformlyScale = true;
    private Point3d _scale = new Point3d(1.0, 1.0, 1.0);
    private bool _promptRotation;
    private double _rotation;
    private static bool _showHiddenBlockDefinitions;
    private ObservableCollection<InsertInstanceOptions> _blockList = new ObservableCollection<InsertInstanceOptions>();
    private int _blockListSelectedIndex = -1;
    private bool _blockDescriptionIsReadOnly = true;
    #endregion Private members
  }
}
