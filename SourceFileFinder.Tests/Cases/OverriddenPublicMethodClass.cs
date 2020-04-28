namespace ReflectionHelpers.Cases
{
    public class OverriddenPublicMethodClass : PublicMethodClass
    {
        public override void Foo() => base.Foo();
    }
}
