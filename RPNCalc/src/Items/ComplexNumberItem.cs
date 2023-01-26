using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace RPNCalc.Items
{
    [DebuggerDisplay("ComplexNumber({value.Real}, {value.Imaginary}i)")]
    public class ComplexNumberItem : AItem<Complex>, IEquatable<Complex>
    {
        public ComplexNumberItem(Complex value) : base(Type.ComplexNumber, value) { }
        public ComplexNumberItem(double real, double imaginary) : base(Type.ComplexNumber, new(real, imaginary)) { }

        public static implicit operator ComplexNumberItem(Complex complex) => new(complex);
        public static implicit operator ComplexNumberItem(RealNumberItem real) => new(real.value, 0);
        public static implicit operator Complex(ComplexNumberItem complex) => complex.value;
        public static implicit operator ComplexNumberItem(int number) => new(number, 0);
        public static implicit operator ComplexNumberItem(long number) => new(number, 0);
        public static implicit operator ComplexNumberItem(double number) => new(number, 0);
        public static implicit operator ComplexNumberItem(float number) => new(number, 0);

        public override bool Equals(AItem other) =>
            (other is ComplexNumberItem complex && complex.value.Equals(value))
            || (value.Imaginary == 0 && other is RealNumberItem number && number.value.Equals(value.Real));

        public bool Equals(Complex other) => value.Equals(other);

        public override string ToString() => $"( {value.Real.ToString(CultureInfo.InvariantCulture)} {value.Imaginary.ToString(CultureInfo.InvariantCulture)} )";
    }
}
