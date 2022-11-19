namespace Razomy.Unity.Scripts.Common.Threading.Tasks
{
  public interface ICancelToken
  {
    bool Cancelled { get; }
  }
}