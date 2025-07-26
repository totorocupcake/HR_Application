namespace HR_Application.Components.Pages
{
    public partial class Counter
    {
        private int currentCount = 0;

        public Counter()
        {
        }

        protected override async Task OnInitializedAsync()
        {
            currentCount = testService.GetValue();
            await base.OnInitializedAsync();
        }
    }
}
