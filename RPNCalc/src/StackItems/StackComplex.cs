using System;
using System.Data.Common;
using System.Diagnostics;
using System.Numerics;

namespace RPNCalc.StackItems
{
    [DebuggerDisplay("ComplexNumber({value.Real}, {value.Imaginary}i)")]
    public class StackComplex : AStackItem<Complex>, IEquatable<Complex>
    {
        public StackComplex(Complex value) : base(Type.ComplexNumber, value) { }
        public StackComplex(double real, double imaginary) : base(Type.ComplexNumber, new(real, imaginary)) { }

        public static implicit operator StackComplex(Complex complex) => new(complex);
        public static implicit operator StackComplex(StackNumber real) => new(real.value, 0);
        public static implicit operator Complex(StackComplex complex) => complex.value;
        public static implicit operator StackComplex(int number) => new(number, 0);
        public static implicit operator StackComplex(long number) => new(number, 0);
        public static implicit operator StackComplex(double number) => new(number, 0);
        public static implicit operator StackComplex(float number) => new(number, 0);

        public override bool Equals(AStackItem other) => (other is StackComplex complex && complex.value == value) || (other is StackNumber number && number.value == value);
        public bool Equals(Complex other) => value.Equals(other);

        public override string ToString() => value.ToString();
    }
}
