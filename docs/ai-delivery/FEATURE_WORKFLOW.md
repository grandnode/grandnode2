# Feature Delivery Workflow

This repository uses an AI-assisted delivery workflow.

## Required Steps

### 1. Brainstorm

The AI assistant should first explore possible approaches and risks.

### 2. Plan

Before code changes, the AI assistant must produce a short implementation plan.

The plan should include:

- files likely to change
- tests to add or update
- validation commands
- risks

### 3. TDD

Where practical, tests should be written before production code.

Expected flow:

1. Add failing test
2. Implement feature
3. Make test pass
4. Refactor only if necessary

### 4. Implementation

Implementation must be minimal and focused on the feature.

Avoid unrelated cleanup.

### 5. Validation

Run relevant commands, for example:

    dotnet restore GrandNode.sln
    dotnet build GrandNode.sln
    dotnet test GrandNode.sln

If tests cannot run locally, document the reason.

### 6. Git

Use a feature branch.

Commit messages should clearly describe the change.
