using System;
using NUnit.Framework;

namespace Batch.Toolkit.CSharp.Tests
{
    public class Create<T>
    {
        private Create(T value, string name)
        {
            Value = value;
            Name = name;
            if (!(string.IsNullOrWhiteSpace(Name)))
            {
                Console.WriteLine($"{Name} was created with value {Value}.");
            }
        }

        public T Value { get; }
        public string Name { get; }

        public Create<TR> Bind<TR>(Func<T, Create<TR>> f)
        {
            try
            {
                if (Value == null)
                {
                    Console.WriteLine($"{Name ?? "<anonymous>"} was null. Aborting!");
                    return null;
                }

                if (!(string.IsNullOrWhiteSpace(Name)))
                {
                    Console.WriteLine($"{Name} had value {Value}. Binding...");
                }

                var result = f(Value);
                Assert.IsNotNull(result);
                return result;
            }
            catch
            {
                return null;
            }
        }

        public Create<TR> Select<TR>(Func<T, TR> f) => Bind(v => Return(f(v)));

        public Create<TR> SelectMany<TR>(Func<T, Create<TR>> f) => Bind(f);

        public Create<TV> SelectMany<TU, TV>(Func<T, Create<TU>> f, Func<T, TU, TV> s)
            => SelectMany(x => f(x).Select(y => s(x, y)));

        public static Create<TR> Return<TR>(TR value, string name = null) => new Create<TR>(value, name);

        public static implicit operator Create<T>(T value) => Return(value);
    }
    public static class CreateMonadExtensions
    {
        public static Create<T> Lift<T>(this T value, string name = null) => Create<T>.Return(value, name);
    }
}