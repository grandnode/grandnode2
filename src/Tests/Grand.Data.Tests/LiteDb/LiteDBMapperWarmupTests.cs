using Grand.Data.LiteDb;
using Grand.Domain;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using LiteDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;
using System.Reflection;

namespace Grand.Data.Tests.LiteDb;

[TestClass]
public class LiteDBMapperWarmupTests
{
    private const int Rounds = 40;

    [TestMethod]
    public void Warm_FirstConcurrentReadsOfAType_ReadTheWholeObject()
    {
        var document = new BsonMapper().ToDocument(new Order {
            OrderNumber = 7,
            CustomerEmail = "buyer@firm.test",
            OrderItems = { new OrderItem { Sku = "SKU-1", Quantity = 2 } }
        });
        var failures = new ConcurrentBag<string>();
        var threadCount = Math.Max(8, Environment.ProcessorCount);

        for (var round = 0; round < Rounds; round++)
        {
            var mapper = new BsonMapper();
            LiteDBMapperWarmup.Warm(mapper, [typeof(Order)]);

            using var barrier = new Barrier(threadCount);
            var threads = Enumerable.Range(0, threadCount).Select(_ => new Thread(() =>
            {
                barrier.SignalAndWait();
                try
                {
                    var order = mapper.ToObject<Order>(document);
                    if (order.OrderNumber != 7 || order.CustomerEmail != "buyer@firm.test" ||
                        order.OrderItems.Count != 1 || order.OrderItems.First().Sku != "SKU-1")
                        failures.Add("incomplete object");
                }
                catch (Exception exc)
                {
                    failures.Add(exc.Message);
                }
            })).ToList();

            threads.ForEach(thread => thread.Start());
            threads.ForEach(thread => thread.Join());
        }

        Assert.IsEmpty(failures, string.Join(" | ", failures.Distinct()));
    }

    [TestMethod]
    public void Warm_ReachesEveryTypeNestedInAnEntity()
    {
        var mapper = new BsonMapper();

        LiteDBMapperWarmup.Warm(mapper, [typeof(WarmRoot)]);

        var mapped = MappedTypes(mapper);
        CollectionAssert.IsSubsetOf(
            new[] {
                typeof(WarmRoot), typeof(WarmListItem), typeof(WarmArrayItem), typeof(WarmDictionaryValue),
                typeof(WarmStruct), typeof(WarmDeep)
            },
            mapped);
    }

    [TestMethod]
    public void Warm_ReachesBaseTypesAndInterfaces_WhichLinqResolvesMembersOn()
    {
        var mapper = new BsonMapper();

        LiteDBMapperWarmup.Warm(mapper, [typeof(WarmRoot)]);

        var mapped = MappedTypes(mapper);
        CollectionAssert.Contains(mapped, typeof(WarmBase));
        CollectionAssert.Contains(mapped, typeof(IWarmContract));
    }

    [TestMethod]
    public void Warm_FirstConcurrentQueriesByInheritedId_DoNotFail()
    {
        var failures = new ConcurrentBag<string>();
        var threadCount = Math.Max(8, Environment.ProcessorCount);

        for (var round = 0; round < Rounds; round++)
        {
            var mapper = new BsonMapper();
            LiteDBMapperWarmup.Warm(mapper, [typeof(Order)]);

            using var barrier = new Barrier(threadCount);
            var threads = Enumerable.Range(0, threadCount).Select(_ => new Thread(() =>
            {
                barrier.SignalAndWait();
                try
                {
                    mapper.GetExpression<Order, bool>(x => x.Id == "1");
                }
                catch (Exception exc)
                {
                    failures.Add(exc.Message);
                }
            })).ToList();

            threads.ForEach(thread => thread.Start());
            threads.ForEach(thread => thread.Join());
        }

        Assert.IsEmpty(failures, string.Join(" | ", failures.Distinct()));
    }

    [TestMethod]
    public void Warm_LeavesSimpleAndFrameworkTypesAlone()
    {
        var mapper = new BsonMapper();

        LiteDBMapperWarmup.Warm(mapper, [typeof(WarmRoot)]);

        var mapped = MappedTypes(mapper);
        CollectionAssert.DoesNotContain(mapped, typeof(string));
        CollectionAssert.DoesNotContain(mapped, typeof(DateTime));
        CollectionAssert.DoesNotContain(mapped, typeof(WarmKind));
        CollectionAssert.DoesNotContain(mapped, typeof(List<WarmListItem>));
        CollectionAssert.DoesNotContain(mapped, typeof(Uri));
        CollectionAssert.DoesNotContain(mapped, typeof(object));
        CollectionAssert.DoesNotContain(mapped, typeof(IDisposable));
    }

    [TestMethod]
    public void Warm_EveryDomainEntity_BuildsTheirNestedTypes()
    {
        var mapper = new BsonMapper();
        var entities = typeof(BaseEntity).Assembly.GetTypes()
            .Where(type => typeof(BaseEntity).IsAssignableFrom(type) && !type.IsAbstract);

        LiteDBMapperWarmup.Warm(mapper, entities);

        var mapped = MappedTypes(mapper);
        CollectionAssert.Contains(mapped, typeof(Order));
        CollectionAssert.Contains(mapped, typeof(OrderItem));
        CollectionAssert.Contains(mapped, typeof(TranslationEntity));
        CollectionAssert.Contains(mapped, typeof(ParentEntity));
    }

    private static List<Type> MappedTypes(BsonMapper mapper)
    {
        var entities = typeof(BsonMapper).GetField("_entities", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return ((System.Collections.IDictionary)entities.GetValue(mapper)!).Keys.Cast<Type>().ToList();
    }

    private enum WarmKind
    {
        One
    }

    private interface IWarmContract
    {
        string Code { get; set; }
    }

    private abstract class WarmBase
    {
        public string Key { get; set; }
    }

    private class WarmRoot : WarmBase, IWarmContract, IDisposable
    {
        public string Code { get; set; }
        public void Dispose() { }
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public WarmKind Kind { get; set; }
        public Uri Link { get; set; }
        public IList<WarmListItem> Items { get; set; } = new List<WarmListItem>();
        public WarmArrayItem[] Array { get; set; }
        public Dictionary<string, WarmDictionaryValue> Values { get; set; }
        public WarmStruct? Maybe { get; set; }
        //a type that contains itself must not loop forever
        public WarmRoot Parent { get; set; }
    }

    private class WarmListItem
    {
        public WarmDeep Deep { get; set; }
    }

    private class WarmArrayItem;

    private class WarmDictionaryValue;

    private struct WarmStruct;

    private class WarmDeep;
}
