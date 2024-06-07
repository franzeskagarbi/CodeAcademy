namespace CodeAcademy.Models
{
    public class ClearSession
    {
        private readonly RequestDelegate _next;
        private static bool _sessionsCleared = false;

        public ClearSession(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_sessionsCleared)
            {
                context.Session.Clear();
                _sessionsCleared = true;
            }

            await _next(context);
        }
    }
}
