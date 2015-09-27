using System.Threading.Tasks;

namespace HB
{
    public static class TaskExtensionMethods
    {
        public static bool IsWaitingToRun(this Task task)
        {
            switch (task.Status)
            {
                case TaskStatus.WaitingForActivation:
                case TaskStatus.WaitingToRun:
                case TaskStatus.Created:
                    return true;
                default:
                    return false;
            }
        }
    }
}