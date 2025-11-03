using System;
using System.Threading;
using Cysharp.Threading.Tasks;


public interface IService : IDisposable
{
	UniTask InitializeAsync(CancellationToken cancellationToken);
}