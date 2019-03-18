using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Nito;
using Xunit;

namespace UnitTests
{
    public class TryUnitTests
    {
        [Fact]
        public void IsValue_ForValue_IsTrue()
        {
            var t = Try.FromValue(13);
            Assert.True(t.IsValue);
        }

        [Fact]
        public void IsValue_ForException_IsFalse()
        {
            var t = Try.FromException<int>(new InvalidOperationException("test"));
            Assert.False(t.IsValue);
        }

        [Fact]
        public void Value_ForValue_IsValue()
        {
            var t = Try.FromValue(13);
            Assert.Equal(13, t.Value);
        }

        [Fact]
        public void Value_ForException_Throws()
        {
            var t = Try.FromException<int>(new InvalidOperationException("test"));
            var ex = Assert.Throws<InvalidOperationException>(() => t.Value);
            Assert.Equal("test", ex.Message);
        }

        [Fact]
        public void IsException_ForValue_IsFalse()
        {
            var t = Try.FromValue(13);
            Assert.False(t.IsException);
        }

        [Fact]
        public void IsException_ForException_IsTrue()
        {
            var t = Try.FromException<int>(new InvalidOperationException("test"));
            Assert.True(t.IsException);
        }

        [Fact]
        public void Exception_ForValue_IsNull()
        {
            var t = Try.FromValue(13);
            Assert.Null(t.Exception);
        }

        [Fact]
        public void Exception_ForException_IsException()
        {
            var t = Try.FromException<int>(new InvalidOperationException("test"));
            var ex = t.Exception;
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Equal("test", ex.Message);
        }

        [Fact]
        public void ToString_ForValue_ContainsValue()
        {
            var t = Try.FromValue(13);
            Assert.Contains("13", t.ToString());
        }

        [Fact]
        public void ToString_ForException_ContainsExceptionMessage()
        {
            var t = Try.FromException<int>(new InvalidOperationException("test"));
            Assert.Contains("test", t.ToString());
        }

        [Fact]
        public void FromException_ExceptionIsNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Try.FromException<int>(null));
        }

        [Fact]
        public void Deconstruct_ForValue_OnlyDeconstructsValue()
        {
            var t = Try.FromValue(13);
            var (ex, value) = t;
            Assert.Null(ex);
            Assert.Equal(13, value);
        }

        [Fact]
        public void Deconstruct_ForException_OnlyDeconstructsException()
        {
            var t = Try.FromException<int>(new InvalidOperationException("test"));
            var (ex, value) = t;
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Equal("test", ex.Message);
            Assert.Equal(default, value);
        }

        [Fact]
        public void Match_ForValue_OnlyExecutesValueFunc()
        {
            var exceptionInvoked = false;
            Exception exception = null;
            var valueInvoked = false;
            int? value = 0;
            var t = Try.FromValue(13);
            var result = t.Match(
                ex =>
                {
                    exceptionInvoked = true;
                    exception = ex;
                    return 7;
                },
                v =>
                {
                    valueInvoked = true;
                    value = v;
                    return 5;
                });
            Assert.False(exceptionInvoked);
            Assert.True(valueInvoked);
            Assert.Equal(13, value);
            Assert.Equal(5, result);
        }

        [Fact]
        public void Match_ForException_OnlyExecutesExceptionFunc()
        {
            var exceptionInvoked = false;
            Exception exception = null;
            var valueInvoked = false;
            int? value = 0;
            var t = Try.FromException<int>(new InvalidOperationException("test"));
            var result = t.Match(
                ex =>
                {
                    exceptionInvoked = true;
                    exception = ex;
                    return 7;
                },
                v =>
                {
                    valueInvoked = true;
                    value = v;
                    return 5;
                });
            Assert.True(exceptionInvoked);
            Assert.False(valueInvoked);
            Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal("test", exception.Message);
        }

        [Fact]
        public void Match_FuncThrows_PropagatesException()
        {
            var t = Try.FromValue(13);
            Assert.Throws<InvalidOperationException>(() => t.Match(
                ex => 7,
                v =>
                {
                    throw new InvalidOperationException("test");
                    return 5;
                }));
        }

        [Fact]
        public void Create_ReturnsValue_CreatesValue()
        {
            var t = Try.Create(() => 13);
            Assert.True(t.IsValue);
        }

        [Fact]
        public void Create_Throws_CreatesException()
        {
            var t = Try.Create(() =>
            {
                throw new InvalidOperationException("test");
                return 13;
            });
            Assert.True(t.IsException);
        }

        [Fact]
        public async Task CreateAsync_ReturnsValue_CreatesValue()
        {
            var t = await Try.Create(async () =>
            {
                await Task.Yield();
                return 13;
            });
            Assert.True(t.IsValue);
        }

        [Fact]
        public async Task CreateAsync_Throws_CreatesException()
        {
            var t = await Try.Create(async () =>
            {
                await Task.Yield();
                throw new InvalidOperationException("test");
                return 13;
            });
            Assert.True(t.IsException);
        }

        [Fact]
        public void Map_ForValue_MapperDoesNotThrow_MapsValue()
        {
            var t = Try.FromValue(13);
            var u = t.Map(x => x * 2);
            Assert.Equal(26, u.Value);
        }

        [Fact]
        public void Map_ForException_IsException()
        {
            var t = Try.FromException<int>(new InvalidOperationException("test"));
            var u = t.Map(x => x * 2);
            Assert.True(u.IsException);
            Assert.IsType<InvalidOperationException>(u.Exception);
            Assert.Equal("test", u.Exception.Message);
        }

        [Fact]
        public void Map_ForException_MapperIsNotCalled()
        {
            var invoked = false;
            var t = Try.FromException<int>(new InvalidOperationException("test"));
            var u = t.Map(x =>
            {
                invoked = true;
                return x * 2;
            });
            Assert.False(invoked);
        }

        [Fact]
        public void Map_ForValue_MapperThrows_IsException()
        {
            var t = Try.FromValue(13);
            var u = t.Map(x =>
            {
                throw new InvalidOperationException("test");
                return x * 2;
            });
            Assert.True(u.IsException);
            Assert.IsType<InvalidOperationException>(u.Exception);
            Assert.Equal("test", u.Exception.Message);
        }

        [Fact]
        public async Task MapAsync_ForValue_MapperDoesNotThrow_MapsValue()
        {
            var t = Try.FromValue(13);
            var u = await t.Map(async x =>
            {
                await Task.Yield();
                return x * 2;
            });
            Assert.Equal(26, u.Value);
        }

        [Fact]
        public async Task MapAsync_ForException_IsException()
        {
            var t = Try.FromException<int>(new InvalidOperationException("test"));
            var u = await t.Map(async x =>
            {
                await Task.Yield();
                return x * 2;
            });
            Assert.True(u.IsException);
            Assert.IsType<InvalidOperationException>(u.Exception);
            Assert.Equal("test", u.Exception.Message);
        }

        [Fact]
        public async Task MapAsync_ForException_MapperIsNotCalled()
        {
            var invoked = false;
            var t = Try.FromException<int>(new InvalidOperationException("test"));
            var u = await t.Map(async x =>
            {
                invoked = true;
                await Task.Yield();
                return x * 2;
            });
            Assert.False(invoked);
        }

        [Fact]
        public async Task MapAsync_ForValue_MapperThrows_IsException()
        {
            var t = Try.FromValue(13);
            var u = await t.Map(async x =>
            {
                await Task.Yield();
                throw new InvalidOperationException("test");
                return x * 2;
            });
            Assert.True(u.IsException);
            Assert.IsType<InvalidOperationException>(u.Exception);
            Assert.Equal("test", u.Exception.Message);
        }

        [Fact]
        public void Select_EnablesLinq()
        {
            var t = Try.FromValue(13);
            var result = from i in t select i + 5;
            Assert.Equal(18, result.Value);
        }

        [Fact]
        public void SelectMany_EnablesLinq()
        {
            var t = Try.FromValue(13);
            var u = Try.FromValue(7);
            var result = from i in t from j in u select i + j;
            Assert.Equal(20, result.Value);
        }
    }
}
