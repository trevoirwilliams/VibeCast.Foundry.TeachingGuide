---
name: prepare-teaching-checkpoint
description: Prepare a VibeCast course branch and code change for a focused live demonstration and reliable learner checkpoint.
---

# Prepare a Teaching Checkpoint

Confirm:

1. The start branch represents the exact beginning of the lesson.
2. The diff implements one observable lesson objective.
3. Setup work that does not teach the objective is already prepared.
4. No future-section capability was introduced.
5. The demonstration can be completed in approximately 8 to 12 minutes.
6. The application or tests visibly validate the result.
7. README or lesson notes include:
   - starting branch;
   - completion branch;
   - commands;
   - configuration requirements;
   - expected output;
   - cleanup requirements.
8. Secrets and instructor-specific resource identifiers are absent.
9. Build and test succeed.
10. The final diff is small enough to explain line by line.

Return:

- demonstration sequence;
- files to have open;
- commands to run;
- expected checkpoints;
- likely failure points;
- final validation statement.