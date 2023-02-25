namespace Azure.Table.Repository.Builders
{
    using System;

    internal class MemberVisitorFactory
    {
        private readonly Func<MemberVisitor> _internalfactory;

        public MemberVisitorFactory(Func<MemberVisitor> func)
        {
            _internalfactory = func;
        }

        public MemberVisitor Create() => _internalfactory();
    }
}
