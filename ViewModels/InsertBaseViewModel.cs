using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;
using Rhino.Display;

namespace Insert.ViewModels
{
  abstract class InsertBaseViewModel : Rhino.ViewModel.NotificationObject
  {
    public InsertBaseViewModel(Rhino.RhinoDoc doc)
    {
      _doc = doc;
    }

    #region Abstract methods
    abstract protected void CreatePreviewImage();
    #endregion Abstract methods

    #region Public properties
    /// <summary>
    /// Document associated with this instance of the view model
    /// </summary>
    /// <value>The document.</value>
    public RhinoDoc Document { get { return _doc; } }
    #endregion Public properties

    #region Public preview image display mode and projection properties
#if ON_OS_WINDOWS
    public System.Drawing.Bitmap PreviewBitmap
    {
      get { return _previewBitmap; }
      set
      {
        if (value == _previewBitmap) return;
        _previewBitmap = value;
        RaisePropertyChanged(() => PreviewBitmap);
      }
    }
#endif
    /// <summary>
    /// Preview image control display mode
    /// </summary>
    public DisplayMode PreviewDisplayMode
    {
      get { return _previewDisplayMode; }
      set
      {
        if (value == _previewDisplayMode) return;
        var previous = _previewDisplayMode;
        _previewDisplayMode = value;
        RaisePropertyChanged(() => PreviewDisplayMode);
        if (previous == DisplayMode.Wireframe) RaisePropertyChanged(() => isWireframeChecked);
        if (previous == DisplayMode.Shaded) RaisePropertyChanged(() => isShadedChecked);
        if (previous == DisplayMode.RenderPreview) RaisePropertyChanged(() => isRenderedChecked);
        CreatePreviewImage();
      }
    }
    /// <summary>
    /// Check the Wireframe preview image context menu item 
    /// </summary>
    public bool isWireframeChecked
    {
      get { return (PreviewDisplayMode == DisplayMode.Wireframe); }
      set { if (value) PreviewDisplayMode = DisplayMode.Wireframe; }
    }
    /// <summary>
    /// Check the shaded preview image context menu item 
    /// </summary>
    public bool isShadedChecked
    {
      get { return (PreviewDisplayMode == DisplayMode.Shaded); }
      set { if (value) PreviewDisplayMode = DisplayMode.Shaded; }
    }
    /// <summary>
    /// Check the rendered preview image context menu item 
    /// </summary>
    public bool isRenderedChecked
    {
      get { return (PreviewDisplayMode == DisplayMode.RenderPreview); }
      set { if (value) PreviewDisplayMode = DisplayMode.RenderPreview; }
    }
    /// <summary>
    /// Preview image projection
    /// </summary>
    public DefinedViewportProjection PreviewProjection
    {
      get { return _previewProjection; }
      set
      {
        if (value == _previewProjection) return;
        var previous = _previewProjection;
        _previewProjection = value;
        RaisePropertyChanged(() => PreviewProjection);
        if (previous == DefinedViewportProjection.Top) RaisePropertyChanged(() => isTopChecked);
        if (previous == DefinedViewportProjection.Bottom) RaisePropertyChanged(() => isBottomChecked);
        if (previous == DefinedViewportProjection.Left) RaisePropertyChanged(() => isLeftChecked);
        if (previous == DefinedViewportProjection.Right) RaisePropertyChanged(() => isRightChecked);
        if (previous == DefinedViewportProjection.Front) RaisePropertyChanged(() => isFrontChecked);
        if (previous == DefinedViewportProjection.Back) RaisePropertyChanged(() => isBackChecked);
        if (previous == DefinedViewportProjection.Perspective) RaisePropertyChanged(() => isPerspectiveChecked);
        CreatePreviewImage();
      }
    }
    /// <summary>
    /// Check the top view image context menu item 
    /// </summary>
    public bool isTopChecked
    {
      get { return (PreviewProjection == DefinedViewportProjection.Top); }
      set { if (value) PreviewProjection = DefinedViewportProjection.Top; }
    }
    /// <summary>
    /// Check the bottom view image context menu item 
    /// </summary>
    public bool isBottomChecked
    {
      get { return (PreviewProjection == DefinedViewportProjection.Bottom); }
      set { if (value) PreviewProjection = DefinedViewportProjection.Bottom; }
    }
    /// <summary>
    /// Check the left view image context menu item 
    /// </summary>
    public bool isLeftChecked
    {
      get { return (PreviewProjection == DefinedViewportProjection.Left); }
      set { if (value) PreviewProjection = DefinedViewportProjection.Left; }
    }
    /// <summary>
    /// Check the right view image context menu item 
    /// </summary>
    public bool isRightChecked
    {
      get { return (PreviewProjection == DefinedViewportProjection.Right); }
      set { if (value) PreviewProjection = DefinedViewportProjection.Right; }
    }
    /// <summary>
    /// Check the front view image context menu item 
    /// </summary>
    public bool isFrontChecked
    {
      get { return (PreviewProjection == DefinedViewportProjection.Front); }
      set { if (value) PreviewProjection = DefinedViewportProjection.Front; }
    }
    /// <summary>
    /// Check the back view image context menu item 
    /// </summary>
    public bool isBackChecked
    {
      get { return (PreviewProjection == DefinedViewportProjection.Back); }
      set { if (value) PreviewProjection = DefinedViewportProjection.Back; }
    }
    /// <summary>
    /// Check the perspective view image context menu item 
    /// </summary>
    public bool isPerspectiveChecked
    {
      get { return (PreviewProjection == DefinedViewportProjection.Perspective); }
      set { if (value) PreviewProjection = DefinedViewportProjection.Perspective; }
    }
    #endregion Public preview image display mode and projection properties

    #region Private members
    /// <summary>
    /// The document used to create the new point
    /// </summary>
    private readonly RhinoDoc _doc;
    private static DisplayMode _previewDisplayMode = DisplayMode.Wireframe;
    private static DefinedViewportProjection _previewProjection = DefinedViewportProjection.Perspective;
#if ON_OS_WINDOWS
    System.Drawing.Bitmap _previewBitmap;
#endif
    #endregion Private members
  }
}
