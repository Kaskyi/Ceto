using System.Collections;

namespace Razomy.Unity.Scripts.Common.Threading.Scheduler
{
  public interface ICoroutine
  {
    void RunCoroutine(IEnumerator e);
  }
}