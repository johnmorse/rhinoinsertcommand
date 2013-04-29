using System;
using System.Reflection;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;

// Extracted and modified from Microsoft.Practices.Prism.ViewModel.NotificationObject
// http://msdn.microsoft.com/en-us/library/gg405484(v=pandp.40).aspx

namespace Rhino.ViewModel
{
  [Serializable]
  abstract class NotificationObject : INotifyPropertyChanged
  {
    /// <summary>
    /// Raised when a property on this object has a new value.
    /// </summary>        
    [field: NonSerialized]
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Raises this object's <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The property that has a new value.</param>
    [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Justification = "Method used to raise an event")]
    protected virtual void RaisePropertyChanged(string propertyName)
    {
      var handler = PropertyChanged;
      if (handler != null)
        handler(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Raises this object's <see cref="PropertyChanged"/> event for each of the properties.
    /// </summary>
    /// <param name="propertyNames">The properties that have a new value.</param>
    [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Justification = "Method used to raise an event")]
    protected void RaisePropertyChanged(params string[] propertyNames)
    {
      if (propertyNames == null) throw new ArgumentNullException("propertyNames");
      foreach (var name in propertyNames)
        RaisePropertyChanged(name);
    }

    /// <summary>
    /// Raises this object's <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <typeparam name="T">The type of the property that has a new value</typeparam>
    /// <param name="propertyExpression">A Lambda expression representing the property that has a new value.</param>
    [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Justification = "Method used to raise an event")]
    [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Cannot change the signature")]
    protected void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression)
    {
      var propertyName = ExtractPropertyName(propertyExpression);
      RaisePropertyChanged(propertyName);
    }

    /// <summary>
    /// Extracts the property name from a property expression.
    /// </summary>
    /// <typeparam name="T">The object type containing the property specified in the expression.</typeparam>
    /// <param name="propertyExpression">The property expression (e.g. p => p.PropertyName)</param>
    /// <returns>The name of the property.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="propertyExpression"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the expression is:<br/>
    ///     Not a <see cref="MemberExpression"/><br/>
    ///     The <see cref="MemberExpression"/> does not represent a property.<br/>
    ///     Or, the property is static.
    /// </exception>
    [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters"), SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
    public static string ExtractPropertyName<T>(Expression<Func<T>> propertyExpression)
    {
      if (propertyExpression == null)
        throw new ArgumentNullException("propertyExpression");

      var memberExpression = propertyExpression.Body as MemberExpression;
      if (memberExpression == null)
        throw new ArgumentException("The expression is not a member access expression.", "propertyExpression");

      var property = memberExpression.Member as PropertyInfo;
      if (property == null)
        throw new ArgumentException("The member access expression does not access a property.", "propertyExpression");

      var getMethod = property.GetGetMethod(true);
      if (getMethod.IsStatic)
        throw new ArgumentException("The referenced property is a static property.", "propertyExpression");

      return memberExpression.Member.Name;
    }
    /// <summary>
    /// Set property helper, will change the specified member value if the new
    /// value is different and raise propery changed event.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="property"></param>
    /// <param name="value"></param>
    /// <param name="member"></param>
    /// <returns>
    /// Returns true if the value was changed and property changed event was
    /// raised otherwise; returns false.
    /// </returns>
    protected bool SetProperty<T>(Expression<Func<T>> property, bool value, ref bool member)
    {
      if (value == member) return false;
      member = value;
      RaisePropertyChanged(property);
      return true;
    }
    /// <summary>
    /// Set property helper, will change the specified member value if the new
    /// value is different and raise propery changed event.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="property"></param>
    /// <param name="value"></param>
    /// <param name="member"></param>
    /// <returns>
    /// Returns true if the value was changed and property changed event was
    /// raised otherwise; returns false.
    /// </returns>
    protected bool SetProperty<T>(Expression<Func<T>> property, string value, ref string member)
    {
      if (value == member) return false;
      member = value;
      RaisePropertyChanged(property);
      return true;
    }
    /// <summary>
    /// Set property helper, will change the specified member value if the new
    /// value is different and raise propery changed event.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="property"></param>
    /// <param name="value"></param>
    /// <param name="member"></param>
    /// <returns>
    /// Returns true if the value was changed and property changed event was
    /// raised otherwise; returns false.
    /// </returns>
    protected bool SetProperty<T>(Expression<Func<T>> property, int value, ref int member)
    {
      if (value == member) return false;
      member = value;
      RaisePropertyChanged(property);
      return true;
    }
    /// <summary>
    /// Set property helper, will change the specified member value if the new
    /// value is different and raise propery changed event.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="property"></param>
    /// <param name="value"></param>
    /// <param name="member"></param>
    /// <returns>
    /// Returns true if the value was changed and property changed event was
    /// raised otherwise; returns false.
    /// </returns>
    protected bool SetProperty<T>(Expression<Func<T>> property, double value, ref double member)
    {
      if (double.IsNaN(value))
      {
        RaiseInvalidPropertyValue(property);
        return false;
      }
      if (value == member) return false;
      member = value;
      RaisePropertyChanged(property);
      return true;
    }
    /// <summary>
    /// Set property helper, will change the specified member value if the new
    /// value is different and raise propery changed event.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="property"></param>
    /// <param name="value"></param>
    /// <param name="member"></param>
    /// <returns>
    /// Returns true if the value was changed and property changed event was
    /// raised otherwise; returns false.
    /// </returns>
    protected bool SetProperty<T>(Expression<Func<T>> property, Rhino.Geometry.Point3d value, ref Rhino.Geometry.Point3d member)
    {
      if (value == member) return false;
      member = value;
      RaisePropertyChanged(property);
      return true;
    }
#if ON_OS_MAC
    public RhinoMac.Window Window { get; set; }
    /// <summary>
    /// Called just prior to closing the window.
    /// </summary>
    /// <returns><c>true</c>, if the window should close <c>false</c> otherwise.</returns>
    /// <summary>
    /// Called just prior to closing the window.
    /// </summary>
    /// <returns><c>true</c>, if the window should close <c>false</c> otherwise.</returns>
    public virtual bool WindowShouldClose()
    {
      return true;
    }
    /// <summary>
    /// Called when this window will close.
    /// </summary>
    public virtual void WindowWillClose()
    {
    }
    /// <summary>
    /// Trees the node count.
    /// </summary>
    /// <returns>The node count.</returns>
    /// <param name="name">Name.</param>
    /// <param name="indexes">Indexes.</param>
    /// <param name="length">Length.</param>
    ulong TreeNodeCount(string name, ref ulong[] indexes, ulong length)
    {
      return 0L;
    }
#endif

    #region Invalid value timer methods
    /// <summary>
    /// Called to fire property changed notifications from
    /// within a set method when attempting to set the propert
    /// to an invalid value.
    /// </summary>
    /// <param name="sender">Sender.</param>
    /// <param name="args">Arguments.</param>
    void InvalidValueTimerElapsed(object sender, System.Timers.ElapsedEventArgs args)
    {
      _invalidValueTimer.Stop();
      _invalidValueTimer.Enabled = false;
      var property = _invalidValueProperty;
      _invalidValueProperty = null;
      if (!string.IsNullOrWhiteSpace(property))
        RaisePropertyChanged(property);
    }
    /// <summary>
    /// Start the invalid value timer which will raise
    /// the appropriate change notification once the
    /// calling function has a chance to return.
    /// </summary>
    /// <param name="propertyName">Property changed event to raise.</param>
    protected void RaiseInvalidPropertyValue(string propertyName)
    {
      if (string.IsNullOrEmpty(propertyName)) return;
      if (null == _invalidValueTimer)
      {
        // Timer used to raise property changed event when attempting
        // to set a property to in an invalid value, when the timer
        // is fired then a RasiePropertyChanged notification is sent
        // telling the bound control to reset its contents to the
        // previous value.
        _invalidValueTimer = new System.Timers.Timer();
        _invalidValueTimer.Enabled = false;
        _invalidValueTimer.Interval = 1;
        _invalidValueTimer.Elapsed += InvalidValueTimerElapsed;
      }
      _invalidValueProperty = propertyName;
      _invalidValueTimer.Enabled = true;
      _invalidValueTimer.Start();
    }
    /// <summary>
    /// Start the invalid value timer which will raise
    /// the appropriate change notification once the
    /// calling function has a chance to return.
    /// </summary>
    /// <param name="propertyExpression">Property expression.</param>
    /// <typeparam name="T">The 1st type parameter.</typeparam>
    protected void RaiseInvalidPropertyValue<T>(System.Linq.Expressions.Expression<System.Func<T>> propertyExpression)
    {
      RaiseInvalidPropertyValue(ExtractPropertyName(propertyExpression));
    }
    private System.Timers.Timer _invalidValueTimer;
    private string _invalidValueProperty = string.Empty;
    #endregion Invalid value timer methods
  }
}
