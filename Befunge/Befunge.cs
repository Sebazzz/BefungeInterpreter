using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

public abstract class BefungeInstruction
{
    public const string NoResult = null;

    private static Dictionary<char, BefungeInstruction> _instructions = new Dictionary<char, BefungeInstruction>
    {
        ['+'] = new BinaryVoidOperator((a,b) => a + b),
        ['-'] = new BinaryVoidOperator((a,b) => b - a),
        ['*'] = new BinaryVoidOperator((a,b) => a * b),
        ['/'] = new BinaryVoidOperator((a,b) => a != 0 ? b / a : a),
        ['%'] = new BinaryVoidOperator((a,b) => a != 0 ? b%a : a),
        ['`'] = new BinaryVoidOperator((a,b) => b>a ? 1 : 0),
        ['!'] = new UnaryVoidOperator((val) => val == 0 ? 1 : 0),
        ['<'] = new DirectionChangeInstruction('<'),
        ['>'] = new DirectionChangeInstruction('>'),
        ['^'] = new DirectionChangeInstruction('^'),
        ['v'] = new DirectionChangeInstruction('v'),
        ['?'] = new RandomDirectionChangeInstruction(),
        ['_'] = new MoveBasedOnValueInstruction('>', '<'),
        ['|'] = new MoveBasedOnValueInstruction('v', '^'),
        ['"'] = new StringReadInstruction(),
        [':'] = new DuplicateValueInstruction(),
       ['\\'] = new SwapValueInstruction(),
        ['$'] = new DelegateBefungeInstruction(state => state.Stack.Pop()),
        ['.'] = new DelegateBefungeInstruction(state => state.Stack.Pop().ToString("D")),
        [','] = new DelegateBefungeInstruction(state => ((char) state.Stack.Pop()).ToString()),
        ['#'] = new DelegateBefungeInstruction(state => state.Direction.Advance(state)),
        ['p'] = new WriteMemoryInstruction(),
        ['g'] = new ReadMemoryInstruction(),
        ['@'] = new DelegateBefungeInstruction(state => state.SetStopState()),
        [' '] = new DelegateBefungeInstruction((state) => {})
    };

    public void Execute(ExecutionState executionState, out string result)
    {
        if (executionState == null) throw new ArgumentNullException(nameof(executionState));

        result = this.ExecuteCurrent(executionState);
    }

    protected abstract string ExecuteCurrent(ExecutionState executionState);

    private sealed class ReadMemoryInstruction : BefungeInstruction
    {
        protected override string ExecuteCurrent(ExecutionState executionState)
        {
            int y = executionState.Stack.Pop();
            int x = executionState.Stack.Pop();

            char v = executionState.Program[
                (x % executionState.ProgramWidth),
                (y % executionState.ProgramHeight)
            ];

            executionState.Stack.Push(v);

            return NoResult;
        }
    }

    private sealed class WriteMemoryInstruction : BefungeInstruction
    {
        protected override string ExecuteCurrent(ExecutionState executionState)
        {
            int y = executionState.Stack.Pop();
            int x = executionState.Stack.Pop();
            int v = executionState.Stack.Pop();

            executionState.Program[
                (x % executionState.ProgramWidth),
                (y % executionState.ProgramHeight)
            ] = (char)v;

            return NoResult;
        }
    }

    private sealed class DelegateBefungeInstruction : BefungeInstruction
    {
        private readonly Func<ExecutionState, string> _executeFunctor;
        private readonly Action<ExecutionState> _executeAction;
        public DelegateBefungeInstruction(Func<ExecutionState, string> executeFunctor)
        {
            _executeFunctor = executeFunctor;
        }

        public DelegateBefungeInstruction(Action<ExecutionState> executeAction)
        {
            _executeAction = executeAction;
        }

        protected override string ExecuteCurrent(ExecutionState executionState)
        {
            if (this._executeAction != null)
            {
                this._executeAction.Invoke(executionState);
                return NoResult;
            }

            return this._executeFunctor.Invoke(executionState);
        }
    }

    private sealed class SwapValueInstruction : BefungeInstruction
    {
        protected override string ExecuteCurrent(ExecutionState executionState)
        {
            int a = executionState.Stack.Pop();
            int b = executionState.Stack.Pop();
            executionState.Stack.Push(a);
            executionState.Stack.Push(b);
            return NoResult;
        }
    }

    private sealed class DuplicateValueInstruction : BefungeInstruction
    {
        protected override string ExecuteCurrent(ExecutionState executionState)
        {
            int val = executionState.Stack.Pop();
            executionState.Stack.Push(val);
            executionState.Stack.Push(val);
            return NoResult;
        }
    }

    private sealed class StringReadInstruction : BefungeInstruction
    {
        protected override string ExecuteCurrent(ExecutionState executionState)
        {
            executionState.Direction.Advance(executionState);
            while (executionState.CurrentInstruction != '"')
            {
                executionState.Stack.Push(executionState.CurrentInstruction);
                executionState.Direction.Advance(executionState);
            }

            return NoResult;
        }
    }

    private sealed class MoveBasedOnValueInstruction : BefungeInstruction
    {
        private readonly char _directionIfZero;
        private readonly char _directionIfNonZero;

        public MoveBasedOnValueInstruction(char directionIfZero, char directionIfNonZero)
        {
            _directionIfZero = directionIfZero;
            _directionIfNonZero = directionIfNonZero;
        }

        protected override string ExecuteCurrent(ExecutionState executionState)
        {
            int val = executionState.Stack.Pop();

            char dir = val == 0 ? this._directionIfZero : this._directionIfNonZero;

            executionState.Direction.Set(dir);

            return NoResult;
        }
    }

    private sealed class RandomDirectionChangeInstruction : BefungeInstruction
    {
        private static readonly char[] Directions =
        {
            '<', '>', '^', 'v'
        };

        protected override string ExecuteCurrent(ExecutionState executionState)
        {
            executionState.Direction.Set(
                Directions[executionState.Random.Next(0, Directions.Length)]
            );

            return NoResult;
        }
    }

    private sealed class DirectionChangeInstruction : BefungeInstruction
    {
        private readonly char _direction;

        public DirectionChangeInstruction(char direction)
        {
            _direction = direction;
        }

        protected override string ExecuteCurrent(ExecutionState executionState)
        {
            executionState.Direction.Set(this._direction);
            return NoResult;
        }
    }

    private sealed class UnaryVoidOperator : BefungeInstruction
    {
        private readonly Func<int, int> _operator;

        public UnaryVoidOperator(Func<int, int> @operator)
        {
            _operator = @operator;
        }

        protected override string ExecuteCurrent(ExecutionState executionState)
        {
            int val = executionState.Stack.Pop();

            int res = this._operator.Invoke(val);
            executionState.Stack.Push(res);

            return NoResult;
        }
    }

    private sealed class BinaryVoidOperator : BefungeInstruction
    {
        private readonly Func<int, int, int> _operator;

        public BinaryVoidOperator(Func<int, int, int> @operator)
        {
            _operator = @operator;
        }

        protected override string ExecuteCurrent(ExecutionState executionState)
        {
            int a = executionState.Stack.Pop();
            int b = executionState.Stack.Pop();

            executionState.Stack.Push(this._operator(a,b));

            return NoResult;
        }
    }

    private sealed class PushValueInstruction : BefungeInstruction
    {
        private readonly int _value;

        public PushValueInstruction(int value)
        {
            _value = value;
        }

        protected override string ExecuteCurrent(ExecutionState executionState)
        {
            executionState.Stack.Push(this._value);
            return NoResult;
        }
    }

    public static implicit operator BefungeInstruction(Func<ExecutionState, string> functor)
    {
        return new DelegateBefungeInstruction(functor);
    }

    public static BefungeInstruction GetDecodeInstruction(char instruction)
    {
        if (!BefungeInstruction._instructions.TryGetValue(instruction, out BefungeInstruction executor))
        {
            executor = new PushValueInstruction(Char.IsDigit(instruction) ? Int32.Parse(instruction.ToString()) : instruction);
        }

        return executor;
    }
}

public sealed class BefungeInterpreter
{
    private ExecutionState _executionState;

    public int InstructionCount { get; private set; }

    public IEnumerable<string> InterpretStepByStep(string program)
    {
        if (this._executionState != null)
        {
            throw new InvalidOperationException("Currently in process of interpreting a program");
        }

        this._executionState = ExecutionStateFactory.CreateInitialExecutionState(program);

        try
        {
            foreach (string output in InterpretStepByStepCore())
            {
                if (this._executionState.ShouldStop)
                {
                    break;
                }

                yield return output;
            }
        }
        finally
        {
            this._executionState = null;
        }
    }

    private IEnumerable<string> InterpretStepByStepCore()
    {
        Debug.Assert(this._executionState != null,"this._executionState != null");

        ExecutionState executionState = this._executionState;

        // Decode and execute instruction
        while (true)
        {
            char currentInstruction = executionState.CurrentInstruction;
            BefungeInstruction decodedInstruction = BefungeInstruction.GetDecodeInstruction(currentInstruction);
            decodedInstruction.Execute(executionState, out string result);

            this.InstructionCount = ++executionState.InstructionCount;

            if (executionState.ShouldStop)
            {
                break;
            }

            if (result == BefungeInstruction.NoResult)
            {
                executionState.Direction.Advance(executionState);

                continue;
            }

            executionState.Direction.Advance(executionState);

            yield return result;
        }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Codewars contract")]
    public string Interpret(string program)
    {
        StringBuilder sb = new StringBuilder();
        foreach (string output in this.InterpretStepByStep(program))
        {
            sb.Append(output);
        }

        return sb.ToString();
    }
}

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class ExecutionState
{
    private ExecutionPosition _position;

    public char[,] Program { get; }
    public ref ExecutionPosition Position => ref this._position;
    public int InstructionCount { get; set; }

    public int ProgramWidth => this.Program.GetLength(0);
    public int ProgramHeight => this.Program.GetLength(1);

    public char CurrentInstruction => this.Program[this.Position.X, this.Position.Y];

    public Direction Direction { get; } = new Direction();

    public Stack Stack { get; } = new Stack();

    public Random Random { get; } = new Random();

    public bool ShouldStop { get; private set; }

    public void SetStopState() => this.ShouldStop = true;

    private string DebuggerDisplay => $"{(this.ShouldStop ? "[STOPPED] " : "")}{this.CurrentInstruction} {this.Direction} {this.Position} [{this.ProgramWidth}, {this.ProgramHeight}] - M{this.Stack.Size}";

    public ExecutionState(char[,] program)
    {
        Program = program;
    }
}

public sealed class Stack
{
    private readonly Stack<int> _storage = new Stack<int>(128);
    public int Size => this._storage.Count;

    public void Push(int val) => this._storage.Push(val);

    public int Pop()
    {
        if (this._storage.TryPop(out int val)) return val;
        return 0;
    }
}

public static class ExecutionStateFactory
{
    public static ExecutionState CreateInitialExecutionState(string rawProgram)
    {
        char[,] program = null;
        
        using (StringReader stringReader = new StringReader(rawProgram))
        {
            List<string> codeLines = new List<string>(25);
            int lineSize = 0;

            string line;
            while ((line = stringReader.ReadLine()) != null)
            {
                lineSize = line.Length;
                codeLines.Add(line);
            }

            program = new char[lineSize, codeLines.Count];
            for (int y = 0; y < codeLines.Count; y++)
            {
                line = codeLines[y];
                for (int x = 0; x < lineSize; x++)
                {
                    if (x >= line.Length)
                    {
                        throw new InvalidOperationException($"Invalid program: program is not rectangular and is not of size {lineSize} at line {y+1}. Line of instructions: {line}");
                    }

                    program[x, y] = line[x];
                }
            }
        }

        return new ExecutionState(program);
    }
}

public struct ExecutionPosition
{
    public int X { get; set; }
    public int Y { get; set; }

    public override string ToString() => $"({this.X},{this.Y})";
}

public class Direction
{
    private char _state = '>';

    public void Set(char state) => this._state = state;

    public void Advance(ExecutionState executionState)
    {
        switch (this._state)
        {
            case '>':
                executionState.Position.X++;
                WrapAround(executionState);
                break;

            case '<':
                executionState.Position.X--;
                WrapAround(executionState);
                break;

            case '^':
                executionState.Position.Y--;
                WrapAround(executionState);
                break;
                
            case 'v':
                executionState.Position.Y++;
                WrapAround(executionState);
                break;

            default:
                throw new InvalidOperationException("Invalid direction: " + this._state);
        }
    }

    private static void WrapAround(ExecutionState executionState)
    {
        bool WrapCore()
        {
            ref ExecutionPosition pos = ref executionState.Position;

            if (pos.X >= executionState.ProgramWidth)
            {
                pos.X = 0;
                return true;
            }

            if (pos.X < 0)
            {
                pos.X = executionState.ProgramWidth - 1;
                return true;
            }

            if (pos.Y >= executionState.ProgramHeight)
            {
                pos.Y = 0;
                return true;
            }

            if (pos.Y < 0)
            {
                pos.Y = executionState.ProgramHeight - 1;
                return true;
            }

            return false;
        }

        while (WrapCore())
        {
            // Wrap one time more until our state is OK
        }
    }

    public override string ToString() => this._state.ToString();
}