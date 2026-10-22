#!/usr/bin/env bash
# Bir solution ve içinde bir veya daha fazla servis oluşturur.
#
#   ./create_project.sh <SolutionAdı> <Servis1> [Servis2 ...] [-- dotnet new ek parametreleri]
#   ./create_project.sh ECommerce Order Basket Payment
#   ./create_project.sh ECommerce Order Basket -- --database postgres --otel
#
# İlk servis solution'ı (BuildingBlocks, ortak ayarlar, docker-compose...) ile birlikte oluşturur,
# sonraki servisler --service-only ile aynı solution'a eklenir.
set -euo pipefail

if [ "$#" -lt 2 ]; then
    echo "Kullanım: $0 <SolutionAdı> <Servis1> [Servis2 ...] [-- ek parametreler]" >&2
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
    echo "'$SOLUTION_NAME' zaten var, üzerine yazılmayacak." >&2
    exit 1
fi

FIRST="${SERVICES[0]}"
echo "Solution oluşturuluyor: $SOLUTION_NAME (ilk servis: $FIRST)"
dotnet new onion -n "$FIRST" -o "$SOLUTION_NAME" --solution-name "$SOLUTION_NAME" ${EXTRA_ARGS[@]+"${EXTRA_ARGS[@]}"}

for SERVICE_NAME in "${SERVICES[@]:1}"; do
    echo "Servis ekleniyor: $SERVICE_NAME"
    (cd "$SOLUTION_NAME" && dotnet new onion -n "$SERVICE_NAME" --service-only -o . ${EXTRA_ARGS[@]+"${EXTRA_ARGS[@]}"})

    # Template post-actions run with continueOnError; make sure the projects really are in the solution.
    (cd "$SOLUTION_NAME" && find "src/Services/${SERVICE_NAME}Service" -name '*.csproj' -print0 \
        | xargs -0 dotnet sln "$SOLUTION_NAME.slnx" add > /dev/null)
done

echo
echo "Hazır: $SOLUTION_NAME/$SOLUTION_NAME.slnx"
echo "  cd $SOLUTION_NAME && dotnet build && dotnet test"
