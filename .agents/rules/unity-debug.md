---
trigger: always_on
---

# Unity Development Rules

Never guess the cause of a bug.

Before modifying code:

1. Analyze the execution flow.
2. Identify where execution stops.
3. List the three most likely causes.
4. Explain why the chosen cause is the most probable.
5. Only then modify the code.

After every fix:

- Check for NullReferenceException risks.
- Check event subscriptions.
- Check Unity lifecycle methods.
- Check serialized references.
- Check networking synchronization.
- Check compilation.
- Report exactly which files changed.

Never refactor unrelated systems while fixing a bug.

If the cause cannot be determined from the code alone, explicitly state what runtime information is needed instead of making speculative changes.