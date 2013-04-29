using System;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace Insert.ViewModels
{
  class InsertInstanceOptions
  {
    /// <summary>
    /// Default constructor
    /// </summary>
    public InsertInstanceOptions()
    {
      Id = Guid.NewGuid();
      UpdateType = InstanceDefinitionUpdateType.Static;
      LayerStyle = InstanceDefinitionLayerStyle.Active;
    }
    /// <summary>
    /// Copy consturctor
    /// </summary>
    /// <param name="source"></param>
    public InsertInstanceOptions(InsertInstanceOptions source)
    {
      CopyFrom(source);
    }
    /// <summary>
    /// Construct from instance defintion and document Id
    /// </summary>
    /// <param name="idef"></param>
    /// <param name="docId"></param>
    public InsertInstanceOptions(InstanceDefinition idef, int docId)
    {
      Id = Guid.NewGuid();
      DocumentId = docId;
      FromInstanceDefinition(idef);
    }
    /// <summary>
    /// Copy values from another InsertInstanceOptions object
    /// </summary>
    /// <param name="source"></param>
    public void CopyFrom(InsertInstanceOptions source)
    {
      Id = source.Id;
      BlockDescription = source.BlockDescription;
      BlockId = source.BlockId;
      BlockName = source.BlockName;
      DocumentId = source.DocumentId;
      InsertSourceArchive = source.InsertSourceArchive;
      SourceArchive = source.SourceArchive;
      Url = source.Url;
      UrlDescription = source.UrlDescription;
      NeedToDisplayOptionsForm = source.NeedToDisplayOptionsForm;
      UpdateType = source.UpdateType;
      LayerStyle = source.LayerStyle;
      SkipNestedLinkedDefinitions = source.SkipNestedLinkedDefinitions;
    }
    /// <summary>
    /// Initialize this objects properties by evaluating the specified Rhino
    /// Instance Defintion.
    /// </summary>
    /// <param name="idef"></param>
    public void FromInstanceDefinition(InstanceDefinition idef)
    {
      try
      {
        if (null == idef)
        {
          BlockId = Guid.Empty;

          BlockName = string.Empty;
          BlockDescription = string.Empty;
          UrlDescription = string.Empty;
          Url = string.Empty;
          RhinoDoc doc = Document;
          if (null != doc)
            BlockName = doc.InstanceDefinitions.GetUnusedInstanceDefinitionName();
          SourceArchive = string.Empty;
          InsertSourceArchive = false;
        }
        else
        {
          // Id of block to insert
          BlockId = idef.Id;
          // Name of block to insert
          BlockName = idef.Name;
          // Block description
          BlockDescription = idef.Description;
          // URL display string
          UrlDescription = idef.UrlDescription;
          // URL
          Url = idef.Url;
          // Not linked to any external file
          SourceArchive = idef.UpdateType == InstanceDefinitionUpdateType.Linked || idef.UpdateType == InstanceDefinitionUpdateType.LinkedAndEmbedded ? idef.SourceArchive : string.Empty;
          // Synch the UpdateType with the InstanceDefnition
          UpdateType = idef.UpdateType;
          if (UpdateType == InstanceDefinitionUpdateType.Linked)
            LayerStyle = (idef.LayerStyle == InstanceDefinitionLayerStyle.Reference ? InstanceDefinitionLayerStyle.Reference : InstanceDefinitionLayerStyle.Active);
          else
            LayerStyle = InstanceDefinitionLayerStyle.None;
          SkipNestedLinkedDefinitions = idef.SkipNestedLinkedDefinitions;
          InsertSourceArchive = false;
          //InsertSourceArchive = (UpdateType == InsertCommand.UpdateType.Linked);
        }
      }
      catch (Exception ex)
      {
        Rhino.Runtime.HostUtils.ExceptionReport(ex);
      }
    }
    /// <summary>
    /// Initialize this instance and associate it with the specified source
    /// archive.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="blockName"></param>
    /// <param name="updateType"></param>
    /// <param name="layerStyle"></param>
    /// <param name="skipNestedLinkedDefinitions"></param>
    public void FromFileName(string fileName, string blockName, InstanceDefinitionUpdateType updateType, InstanceDefinitionLayerStyle layerStyle, bool skipNestedLinkedDefinitions)
    {
      InsertSourceArchive = !string.IsNullOrWhiteSpace(fileName);
      SourceArchive = fileName;
      BlockName = blockName;
      BlockId = Guid.Empty;
      UpdateType = updateType;
      LayerStyle = layerStyle;
      // Just use the default layer style, it will get validated on they way out and we need it to remember the default style for linked blocks
      //if (UpdateType == InstanceDefinitionUpdateType.Linked)
      //  LayerStyle = (layerStyle == InstanceDefinitionLayerStyle.None ? InstanceDefinitionLayerStyle.Active : layerStyle);
      //else
      //  LayerStyle = InstanceDefinitionLayerStyle.None;
      SkipNestedLinkedDefinitions = skipNestedLinkedDefinitions;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="existingInstanceDefinitions"></param>
    /// <param name="blockName"></param>
    /// <param name="idef"></param>
    public void BlockNameFromFileName(string fileName, InstanceDefinition[] existingInstanceDefinitions, out string blockName, out InstanceDefinition idef)
    {
      blockName = string.Empty;
      idef = null;
      // Get a list of existing InstanceDefinition objects which have a source archive equal to fileName
      if (null == existingInstanceDefinitions)
        existingInstanceDefinitions = InstanceDefintionsFromFileName(fileName);
      if (UpdateType == InstanceDefinitionUpdateType.Linked)
      {
        // If linking to a file check the existing definition list for a linked file
        idef = FindFistLinkedInstanceDefinition(existingInstanceDefinitions);
      }
      // If not linked with a matching liked definition look through the list of InstanceDefinition objects
      // with SourceArchive values equal to fileName for a InstanceDefintion with the same UpdateType,
      // LayerStyle, and SkipNestedLinkedDefinitions values
      for (int i = 0; null == idef && i < existingInstanceDefinitions.Length; i++)
        if (FileNameOptionsMatch(existingInstanceDefinitions[i]))
          idef = existingInstanceDefinitions[i];
      if (null != idef)
      {
        // A matching linked block was found so use it, only ever use one linked definition per file or
        // another InstanceDefinitino with matching properties was found so just use it
        FromInstanceDefinition(idef);
      }
      else
      {
        // This means there was not match found so cook up a block name based on the file name making sure the new name is unique
        blockName = System.IO.Path.GetFileNameWithoutExtension(fileName);
        // Block name is in use so add "01" to end
        if (null != Document.InstanceDefinitions.Find(blockName, true))
          blockName = Document.InstanceDefinitions.GetUnusedInstanceDefinitionName(blockName);
        // Update Command arguments using values specified by the user in the Options form
        FromFileName(fileName, blockName, UpdateType, LayerStyle, SkipNestedLinkedDefinitions);
      }
    }
    /// <summary>
    /// Compare properties that appear in the option form; UpdateType, LayerStyle and SkipNestedLinkedDefinitions flags.
    /// </summary>
    /// <param name="idef"></param>
    /// <returns></returns>
    public bool FileNameOptionsMatch(InstanceDefinition idef)
    {
      return (null != idef && UpdateType == idef.UpdateType && LayerStyle == idef.LayerStyle && SkipNestedLinkedDefinitions == idef.SkipNestedLinkedDefinitions);
    }
    /// <summary>
    /// Search list of InstanceDefinitions and return the first linked one found
    /// </summary>
    /// <param name="idefs"></param>
    /// <returns></returns>
    public InstanceDefinition FindFistLinkedInstanceDefinition(InstanceDefinition[] idefs)
    {
      foreach (var idef in idefs)
        if (idef.UpdateType == InstanceDefinitionUpdateType.Linked)
          return idef;
      return null;
    }
    /// <summary>
    /// Look in the active document for a InstanceDefinition with the specified name and return
    /// the InstanceDefinition found or null if not found.
    /// </summary>
    /// <param name="idefName"></param>
    /// <returns>Returns a InstanceDeinition object with the matching name otherwise return null</returns>
    public InstanceDefinition FindInstanceDefintion(string idefName)
    {
      InstanceDefinition result = null;
      RhinoDoc doc = Document;

      if (null != doc && !string.IsNullOrWhiteSpace(idefName))
      {
        var trimmedIdefName = idefName.Trim();
        var table = doc.InstanceDefinitions;
        for (int i = 0, count = table.Count; null == result && i < count; i++)
        {
          var idef = table[i];
          if (null != idef && !idef.IsDeleted && !idef.IsReference && string.Compare(idef.Name, trimmedIdefName, StringComparison.InvariantCultureIgnoreCase) == 0)
            result = idef;
        }
      }

      return result;
    }
    /// <summary>
    /// Call this method to get a list of all of the InstanceDefinition objects that reference this source file
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public InstanceDefinition[] InstanceDefintionsFromFileName(string fileName)
    {
      List<InstanceDefinition> idefs = new List<InstanceDefinition>();
      if (null != Document && !string.IsNullOrWhiteSpace(fileName))
      {
        string trimmedFileName = fileName.Trim();
        foreach (var idef in Document.InstanceDefinitions)
          if (!idef.IsDeleted && !idef.IsReference
               && string.Compare(idef.SourceArchive, trimmedFileName, StringComparison.InvariantCultureIgnoreCase) == 0
               && (idef.UpdateType == InstanceDefinitionUpdateType.LinkedAndEmbedded || idef.UpdateType == InstanceDefinitionUpdateType.Linked))
            idefs.Add(idef); // only add Linked or LinkedAndEmbedded definitions whose SourceArchive matches
      }
      return idefs.ToArray();
    }
    /// <summary>
    /// Helper used when sorting lists of Instance Options
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    static public int Compare(InsertInstanceOptions a, InsertInstanceOptions b)
    {
      if (a == null && b == null)
        return 0;
      if (a == null)
        return -1;
      if (b == null)
        return 1;
      return string.Compare(a.BlockName, b.BlockName);
    }
    /// <summary>
    /// Returns the block name associated with this item
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
      // 20 August 2012 John Morse http://dev.mcneel.com/bugtrack/?q=112140
      // Updating a referenced file that includes blocks causes the documents block table to contain
      // null Instance Definition pointers, when this happens the Id will be Guid.Empty and the
      // Instance Definition Name property will return NULL which cause the BlockName property, which 
      // calls the Instance Definitions Name property, to return null.  I added a test for null and
      // now return a null string "" when this happens instead of a null so UI components wont throw
      // exceptions.
      string s = BlockName;
      return (s ?? string.Empty);
    }
    #region Preview bitmap
    #if ON_OS_WINDOWS
    /// <summary>
    /// Return the cached bitmap unless the InsertSystemArchive, projection, displayMode or size is different then what
    /// was used to generate the cached preview bitmap.
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="displayMode"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public System.Drawing.Bitmap GetPreviewBitmap(Rhino.Display.DefinedViewportProjection projection, DisplayMode displayMode, System.Drawing.Size size)
    {
      //var x = new System.Windows.Media.Imaging.WriteableBitmap
      bool previewFromFile = ((BlockId == Guid.Empty || InsertSourceArchive) && !string.IsNullOrWhiteSpace(SourceArchive));
      if (null != _previewBitmap)
      {
        if (null != _previewBitmap && _previewBitmap.Size != size)
          ClearPreviewBitmap();
        else if (previewFromFile != _previewFromFile)
          ClearPreviewBitmap();
        else if (!previewFromFile && (projection != _projection || displayMode != _displayMode))
          ClearPreviewBitmap();
      }
      if (null != _previewBitmap)
        return _previewBitmap;
      if (previewFromFile)
      {
        // Extract thumbnail preview image from file
        System.Drawing.Bitmap bitmap = null;
        if (!string.IsNullOrWhiteSpace(SourceArchive) && System.IO.File.Exists(SourceArchive))
          bitmap = Rhino.FileIO.File3dm.ReadPreviewImage(SourceArchive);
        if (null != bitmap && bitmap.Width <= size.Width && bitmap.Height <= size.Height)
        {
          // If the thumbnail is smaller than or equal to the requested size just use it
          _previewBitmap = bitmap;
        }
        else if (null != bitmap)
        {
          // Thumbnail is larger in one dimension so stretch it so it fits side to side or top to bottom, which ever will fit maintaining the image aspect ratio
          float scale = Math.Max((float)bitmap.Width / (float)size.Width, (float)bitmap.Height / (float)size.Height);
          // Create a preview bitmap of the a size that fits using the same aspect ratio as the source imae
          _previewBitmap = new System.Drawing.Bitmap((int)((float)bitmap.Width / scale), (int)((float)bitmap.Height / scale), System.Drawing.Imaging.PixelFormat.Format24bppRgb);
          var g = System.Drawing.Graphics.FromImage(_previewBitmap);
          g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
          // Stretch the source image to fit the new image size (shrinking the source image)
          g.DrawImage(bitmap, 0, 0, _previewBitmap.Width, _previewBitmap.Height);
          bitmap.Dispose();
        }
      }
      else
      {
        InstanceDefinition idef = InstanceDefinition;
        if (null != idef)
        {
          _displayMode = displayMode;
          _projection = projection;
          _previewBitmap = idef.CreatePreviewBitmap(projection, displayMode, size);
        }
      }
      _previewFromFile = previewFromFile;
      return _previewBitmap;
    }
    void ClearPreviewBitmap()
    {
      if (null != _previewBitmap)
        _previewBitmap.Dispose();
      _previewBitmap = null;
    }
    System.Drawing.Bitmap _previewBitmap;
    bool _previewFromFile;
    Rhino.Display.DefinedViewportProjection _projection = Rhino.Display.DefinedViewportProjection.Perspective;
    DisplayMode _displayMode = DisplayMode.Wireframe;
    #endif
    #endregion Preview bitmap
    /// <summary>
    /// Get the default values which should be used when making new instance
    /// defintions.
    /// </summary>
    public static InsertInstanceOptions Defaults
    {
      get
      {
        if (null == _defaults)
          _defaults = new InsertInstanceOptions();
        return _defaults;
      }
    }
    private static InsertInstanceOptions _defaults;
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
    /// Document that contains the instance definition
    /// </summary>
    public int DocumentId { get; set; }
    /// <summary>
    /// Document that contains the instance definition
    /// </summary>
    public RhinoDoc Document { get { return RhinoDoc.FromId(DocumentId); } }
    /// <summary>
    ///
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// The instance definition to insert
    /// </summary>
    public Guid BlockId { get;  set; }
    /// <summary>
    /// The name of the instance definition to insert
    /// </summary>
    public string BlockName { get;  set; }
    /// <summary>
    /// The description field from the instance defintion
    /// </summary>
    public string BlockDescription { get;  set; }
    /// <summary>
    /// The URL description from the instace defintion to insert
    /// </summary>
    public string UrlDescription { get;  set; }
    /// <summary>
    /// The URL string from the instace defintion to insert
    /// </summary>
    public string Url { get;  set; }
    /// <summary>
    /// The full path of the source archive to link to
    /// </summary>
    public string SourceArchive { get;  set; }
    /// <summary>
    /// Include the source archive when inserting the instance
    /// </summary>
    public bool InsertSourceArchive { get; set; }
    public bool NeedToDisplayOptionsForm { get; set; }
    public InstanceDefinitionUpdateType UpdateType { get; set; }
    public InstanceDefinitionLayerStyle LayerStyle { get; set; }
    public bool SkipNestedLinkedDefinitions { get; set; }
  }
  /// <summary>
  /// Controls what kind of geometry is created as a result of the insert
  /// </summary>
  public enum InsertAs
  {
    /// <summary>
    /// Insert instance as instance definition (block)
    /// </summary>
    Block = 0,
    /// <summary>
    /// Insert as individual objects and group them together
    /// </summary>
    ObjectsInGroup = 1,
    /// <summary>
    /// Insert as individual objects (not in a group)
    /// </summary>
    Objects = 2
  };
}
