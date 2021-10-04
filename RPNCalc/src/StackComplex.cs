using System.Diagnostics;
using System.Numerics;

namespace RPNCalc
{
    [DebuggerDisplay("ComplexNumber({value.Real}, {value.Imaginary}i)")]
    public class StackComplex : AStackItem<Complex>
    {
        public StackComplex(Type type) : base(type) { }
        public StackComplex(Complex value) : base(Type.ComplexNumber, value) { }
        public StackComplex(double real, double imaginary) : base(Type.ComplexNumber, new(real, imaginary)) { }

        public override string ToString() => $"({value.Real}, {value.Imaginary})";
    }
}
