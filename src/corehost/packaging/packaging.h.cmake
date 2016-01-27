#include "pal.h"
#define COREHOST_PACKAGE_NAME _X("${COREHOST_PACKAGE_NAME}")
#define COREHOST_PACKAGE_VERSION _X("${COREHOST_PACKAGE_VERSION}")
#define COREHOST_PACKAGE_LIBHOST_RELATIVE_DIR _X("${COREHOST_PACKAGE_LIBHOST_RELATIVE_DIR}")

namespace
{
constexpr bool Equals(pal::char_t const* first, pal::char_t const* second)
{
    return (*first && *second) ? (*first == *second && Equals(first + 1, second + 1)) : (!*first && !*second);
}
static_assert(Equals(COREHOST_PACKAGE_NAME, _X("Microsoft.DotNet.CoreHost")), "Did you update package info and version correctly?");
static_assert(Equals(COREHOST_PACKAGE_VERSION, _X("1.0.0")), "Did you update package info and version correctly?");
}
