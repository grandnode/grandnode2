#!/usr/bin/env bash
#
# Runs every test project under src/Tests, one project at a time.
#
# Discovering the projects instead of listing them keeps CI honest: a new test project
# is picked up automatically rather than silently never running.
#
# Running them one at a time is deliberate. The suites share process-wide static state
# (DataSettingsManager.Instance, PluginPaths.Instance, AutoMapperConfig), so a single
# whole-solution `dotnet test` makes Customers, Marketing and Messages fail depending on
# the order they happen to execute in. Until that state is removed, per-project runs are
# what gives a trustworthy signal.
#
# Usage: run-tests.sh [configuration] [extra dotnet test args...]
#   run-tests.sh Release
#   run-tests.sh Debug --collect:"XPlat Code Coverage"
#
# Every project is run even if an earlier one fails, so a single CI run reports all of
# the broken suites rather than only the first.

set -uo pipefail

configuration="${1:-Release}"
[ $# -gt 0 ] && shift

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"

mapfile -t projects < <(find src/Tests -name '*.csproj' | sort)

if [ ${#projects[@]} -eq 0 ]; then
    echo "No test projects found under src/Tests" >&2
    exit 1
fi

echo "Running ${#projects[@]} test projects in $configuration"

failed=()

for project in "${projects[@]}"; do
    name="$(basename "$project" .csproj)"
    echo "::group::$name"
    if ! dotnet test "$project" --configuration "$configuration" --no-build --nologo "$@"; then
        failed+=("$name")
    fi
    echo "::endgroup::"
done

if [ ${#failed[@]} -gt 0 ]; then
    printf 'FAILED: %s\n' "${failed[@]}" >&2
    echo "::error::Failing test projects: ${failed[*]}"
    exit 1
fi

echo "All ${#projects[@]} test projects passed."
