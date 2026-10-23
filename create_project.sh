#!/usr/bin/env bash
# Creates a solution with one or more services.
#
#   ./create_project.sh <SolutionName> <Service1> [Service2 ...] [-- extra dotnet new arguments]
#   ./create_project.sh ECommerce Order Basket Payment
#   ./create_project.sh ECommerce Order Basket -- --database postgres --otel
#
# The first service is created together with the solution (BuildingBlocks, shared settings, docker-compose...);
# the following services are added with --service-only to the same solution and to the include list of
# docker-compose.yml.
set -euo pipefail

if [ "$#" -lt 2 ]; then
    echo "Usage: $0 <SolutionName> <Service1> [Service2 ...] [-- extra arguments]" >&2
    exit 1
fi

SOLUTION_NAME="$1"
shift

SERVICES=()
while [ "$#" -gt 0 ] && [ "$1" != "--" ]; do
    SERVICES+=("$1")
    shift
done
[ "${1:-}" = "--" ] && shift
EXTRA_ARGS=("$@")

if [ -e "$SOLUTION_NAME" ]; then
    echo "'$SOLUTION_NAME' already exists and will not be overwritten." >&2
    exit 1
fi

FIRST="${SERVICES[0]}"
echo "Creating solution: $SOLUTION_NAME (first service: $FIRST)"
dotnet new onion -n "$FIRST" -o "$SOLUTION_NAME" --solution-name "$SOLUTION_NAME" ${EXTRA_ARGS[@]+"${EXTRA_ARGS[@]}"}

for SERVICE_NAME in "${SERVICES[@]:1}"; do
    echo "Adding service: $SERVICE_NAME"
    (cd "$SOLUTION_NAME" && dotnet new onion -n "$SERVICE_NAME" --service-only -o . ${EXTRA_ARGS[@]+"${EXTRA_ARGS[@]}"})

    # Template post-actions run with continueOnError; make sure the projects really are in the solution.
    (cd "$SOLUTION_NAME" && find "src/Services/${SERVICE_NAME}Service" -name '*.csproj' -print0 \
        | xargs -0 dotnet sln "$SOLUTION_NAME.slnx" add > /dev/null)

    # Register the service's compose.yaml after the last entry of the root include list.
    COMPOSE_FILE="$SOLUTION_NAME/docker-compose.yml"
    awk -v entry="  - src/Services/${SERVICE_NAME}Service/compose.yaml" '
        { lines[NR] = $0 }
        /^  - src\/Services\/.*\/compose\.yaml$/ { last = NR }
        END {
            if (!last) exit 1
            for (i = 1; i <= NR; i++) { print lines[i]; if (i == last) print entry }
        }' "$COMPOSE_FILE" > "$COMPOSE_FILE.tmp" \
        || { rm -f "$COMPOSE_FILE.tmp"; echo "The include list in docker-compose.yml was not found." >&2; exit 1; }
    mv "$COMPOSE_FILE.tmp" "$COMPOSE_FILE"
done

echo
echo "Done: $SOLUTION_NAME/$SOLUTION_NAME.slnx"
echo "  cd $SOLUTION_NAME && dotnet build && dotnet test"
