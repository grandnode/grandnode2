﻿namespace Grand.SharedKernel.Extensions
{
    public static class ExtendedLinq
    {
        public static bool ContainsAny<TSource>(this IEnumerable<TSource> source, IEnumerable<TSource> values)
        {
            return source.Any(s => values.Contains(s));
        }

        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(
                this IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
            return sequences.Aggregate(
              emptyProduct,
              (accumulator, sequence) =>
                from accseq in accumulator
                from item in sequence
                select accseq.Concat(new[] { item }));
        }
    }
}
