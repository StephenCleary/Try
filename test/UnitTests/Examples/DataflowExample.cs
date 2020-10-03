using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Nito;
using Xunit;

namespace UnitTests.Examples
{
    public class DataflowExample
    {
        [Fact]
        public async Task DataflowExampleWithoutTry()
        {
            var inputBlock = new TransformBlock<int, int>(value =>
            {
                if (value % 3 == 0)
                    throw new InvalidOperationException($"Power of 3 found: {value}");
                return value;
            });
            var outputBlock = new TransformBlock<int, int>(async value =>
            {
                await Task.Yield();
                if (value % 2 == 0)
                    throw new InvalidOperationException($"Power of 2 found: {value}");
                return value;
            });
            inputBlock.LinkTo(outputBlock, new DataflowLinkOptions { PropagateCompletion = true });
            inputBlock.Post(1); // comes through fine if you receive it before the block faults
            inputBlock.Post(2); // faults second block
            inputBlock.Post(3); // faults both blocks
            inputBlock.Post(5); // lost, since blocks are faulted (Post returns false if block has already faulted)

            inputBlock.Complete();
            await Assert.ThrowsAnyAsync<Exception>(() => outputBlock.Completion);
            await Assert.ThrowsAnyAsync<Exception>(() => inputBlock.Completion);
            Assert.True(inputBlock.Completion.IsFaulted);
            Assert.True(outputBlock.Completion.IsFaulted);
        }

        [Fact]
        public async Task DataflowExampleWithTry()
        {
            var inputBlock = new TransformBlock<int, Try<int>>(value => Try.Create(() =>
            {
                if (value % 3 == 0)
                    throw new InvalidOperationException($"Power of 3 found: {value}");
                return value;
            }));
            var outputBlock = new TransformBlock<Try<int>, Try<int>>(t => t.Map(async value =>
            {
                await Task.Yield();
                if (value % 2 == 0)
                    throw new InvalidOperationException($"Power of 2 found: {value}");
                return value;
            }));
            inputBlock.LinkTo(outputBlock, new DataflowLinkOptions { PropagateCompletion = true });
            inputBlock.Post(1); // comes through as a value
            inputBlock.Post(2); // comes through as an exception
            inputBlock.Post(3); // comes through as an exception
            inputBlock.Post(5); // comes through as a value

            inputBlock.Complete();
            var result1 = outputBlock.Receive();
            Assert.Equal(1, result1.Value);
            var result2 = outputBlock.Receive();
            Assert.True(result2.IsException);
            var result2Exception = Assert.Throws<InvalidOperationException>(() => result2.Value);
            Assert.Equal("Power of 2 found: 2", result2Exception.Message);
            var result3 = outputBlock.Receive();
            Assert.True(result3.IsException);
            var result3Exception = Assert.Throws<InvalidOperationException>(() => result3.Value);
            Assert.Equal("Power of 3 found: 3", result3Exception.Message);
            var result5 = outputBlock.Receive();
            Assert.Equal(5, result5.Value);

            await outputBlock.Completion; // no faulted blocks
        }
    }
}
