namespace ReflectionHelpers.Cases
{
    public class OverriddenProtectedMethodClass : ProtectedMethodClass
    {
        protected override void Foo() => base.Foo();
    }
}
