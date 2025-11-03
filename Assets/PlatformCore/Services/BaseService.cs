using System.Threading;
using Cysharp.Threading.Tasks;

public abstract class BaseService : IService
{
	private CancellationTokenSource _serviceTokenSource;
	protected CancellationToken ServiceToken => _serviceTokenSource?.Token ?? CancellationToken.None;

	public UniTask InitializeAsync(CancellationToken cancellationToken)
	{
		_serviceTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		return InitializeServiceAsync();
	}

	protected abstract UniTask InitializeServiceAsync();

	protected virtual void DisposeService()
	{
	}

	public void Dispose()
	{
		DisposeService();

		_serviceTokenSource?.Cancel();
		_serviceTokenSource?.Dispose();
		_serviceTokenSource = null;
	}
}