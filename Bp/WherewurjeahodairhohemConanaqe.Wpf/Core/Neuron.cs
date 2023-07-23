﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WherewurjeahodairhohemConanaqe.Wpf.Core;

public class Runner
{
    public Runner(NeuronManager neuronManager)
    {
        NeuronManager = neuronManager;
    }

    public NeuronManager NeuronManager { get; }

    public void Run()
    {
        RunCount++;

        foreach (var neuron in NeuronManager.NeuronList)
        {
            RunNeuron(neuron);
        }
    }

    private void RunNeuron(Neuron neuron)
    {
        neuron.Run();

        var inputArgument = neuron.OutputArgument.ToInput();

        foreach (var outputNeuron in neuron.GetOutputNeuron())
        {
            outputNeuron.InputManager.SetInput(neuron.Id, inputArgument);
        }
    }

    /// <summary>
    /// 执行次数
    /// </summary>
    public ulong RunCount { get; private set; }
}

public class NeuronManager
{
    public NeuronManager()
    {
        var inputNeuron = new InputNeuron();
        NeuronList.Add(inputNeuron);
        var neuron1 = CreateNeuron();
        NeuronList.Add(neuron1);
        var neuron2 = CreateNeuron();
        NeuronList.Add(neuron2);

        inputNeuron.AddOutputNeuron(neuron1);
        neuron1.AddOutputNeuron(neuron2);
    }

    public List<Neuron> NeuronList { get; } = new List<Neuron>();

    public Neuron CreateNeuron()
    {
        var count = Interlocked.Increment(ref _neuronCount);
        return new Neuron(new NeuronId(count));
    }

    private ulong _neuronCount = 1;
}

class InputNeuron : Neuron
{
    public InputNeuron() : base(new NeuronId(0))
    {
    }

    protected override OutputArgument RunCore(Span<InputArgument> inputList)
    {
        // 先使用任意的输入方式
        return new OutputArgument(Random.Shared.NextDouble());
    }
}

/// <summary>
/// 执行状态
/// </summary>
public enum RunStatus
{
    Running,
}

public class Neuron
{
    public Neuron(NeuronId id)
    {
        Id = id;
    }

    public NeuronId Id { get; }
    public NeuronLayerIndex LayerIndex { get; private set; } = new NeuronLayerIndex(0);

    public InputManager InputManager { get; } = new InputManager();
    public OutputArgument OutputArgument { get; protected set; }
    public bool Running { private set; get; }
    /// <summary>
    /// 运行次数
    /// </summary>
    public ulong RunCount { get; protected set; }

    private List<Neuron> OutputNeuronList { get; } = new List<Neuron>();

    public void AddOutputNeuron(Neuron outputNeuron)
    {
        lock (OutputNeuronList)
        {
            OutputNeuronList.Add(outputNeuron);
        }
    }

    public List<Neuron> GetOutputNeuron()
    {
        lock (OutputNeuronList)
        {
            return OutputNeuronList.ToList();
        }
    }

    /// <summary>
    /// 执行
    /// </summary>
    public void Run()
    {
        Running = true;
        RunCount++;

        // 实现最简单的方式，可以替换为不同的方式
        using var input = InputManager.GetInput();
        Span<InputArgument> inputList = input.AsSpan();

        var outputArgument = RunCore(inputList);
        OutputArgument = outputArgument;

        Running = false;
    }

    /// <summary>
    /// 执行内容
    /// </summary>
    protected virtual OutputArgument RunCore(Span<InputArgument> inputList)
    {
        double sum = 0;
        foreach (var inputArgument in inputList)
        {
            sum += inputArgument.Value;
        }

        var output = sum / inputList.Length;

        return new OutputArgument(output);
    }
}

/// <summary>
/// 输入管理
/// </summary>
/// 允许接收多个输入，处理多线程执行的问题
public class InputManager
{
    public void SetInput(NeuronId id, InputArgument argument)
    {
        lock (Locker)
        {
            InputArgumentList[id] = argument;
        }
    }

    public InputWithArrayPool GetInput()
    {
        lock (Locker)
        {
            var count = InputArgumentList.Count;

            var inputList = ArrayPool<InputArgument>.Shared.Rent(count);

            int i = 0;
            foreach (var keyValuePair in InputArgumentList)
            {
                inputList[i] = keyValuePair.Value;

                i++;
            }

            return new InputWithArrayPool(inputList, count, ArrayPool<InputArgument>.Shared);
        }
    }

    private Dictionary<NeuronId, InputArgument> InputArgumentList { get; } = new Dictionary<NeuronId, InputArgument>();

    private object Locker => InputArgumentList;
}

/// <summary>
/// 输入的内容，用于归还数组池。实际应该调用 <see cref="AsSpan"/> 方法使用
/// </summary>
/// <param name="InputList"></param>
/// <param name="Length"></param>
/// <param name="ArrayPool"></param>
public readonly record struct InputWithArrayPool(InputArgument[] InputList, int Length, ArrayPool<InputArgument> ArrayPool) : IDisposable
{
    public Span<InputArgument> AsSpan() => new Span<InputArgument>(InputList, 0, Length);

    public void Dispose()
    {
        ArrayPool.Return(InputList);
    }
}

/// <summary>
/// 进行输入的参数
/// </summary>
/// <param name="Value"></param>
public readonly record struct InputArgument(double Value)
{
}

public readonly record struct OutputArgument(double Value)
{
    public InputArgument ToInput() => new InputArgument(Value);
}

public readonly record struct NeuronId(ulong Value);

/// <summary>
/// 表示多少层
/// </summary>
/// <param name="Value"></param>
public readonly record struct NeuronLayerIndex(ulong Value);