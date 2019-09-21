namespace Das.Serializer
{
    public delegate void VoidMethod(object target, params object[] args);

    public delegate void PropertySetter(ref object target, object value);
}