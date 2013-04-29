using System;

using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace Insert.ViewModels
{
  /// <summary>
  /// 
  /// </summary>
  class InsertInstanceCommandOptions : InsertInstanceOptions
  {
    public InsertInstanceCommandOptions()
    {
      InsertAs = InsertAs.Block;
      PromptForInsertionPoint = true;
      InsertionPoint = Point3d.Origin;
      UniformlyScale = true;
      Scale = new Point3d(1.0, 1.0, 1.0);
      RotationAngle = 0.0;
    }
    public InsertInstanceCommandOptions(InsertInstanceCommandOptions source)
    {
      CopyFrom(source);
    }
    public void CopyFrom(InsertInstanceCommandOptions source)
    {
      DocumentId = source.DocumentId;
      InsertAs = source.InsertAs;
      PromptForInsertionPoint = source.PromptForInsertionPoint;
      InsertionPoint = source.InsertionPoint;
      PromptForScale = source.PromptForScale;
      UniformlyScale = source.UniformlyScale;
      Scale = source.Scale;
      PromptForRotationAngle = source.PromptForRotationAngle;
      RotationAngle = source.RotationAngle;
      NeedToDisplayOptionsForm = source.NeedToDisplayOptionsForm;
      CopyFrom((InsertInstanceOptions)source);
    }
    public InsertAs InsertAs { get; set; }
    public bool PromptForInsertionPoint { get; set; }
    public Point3d InsertionPoint { get; set; }
    public bool PromptForScale { get; set; }
    public bool UniformlyScale { get; set; }
    public Point3d Scale { get; set; }
    public bool PromptForRotationAngle { get; set; }
    public double RotationAngle { get; set; }
  }
}
