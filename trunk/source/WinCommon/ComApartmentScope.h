#pragma once

#include <cstdint>

// PlatformApi supplies the HRESULT-returning Initialize and matching Uninitialize.
// The callback's local COM clients are destroyed before the apartment is released,
// including when the callback throws. S_OK and S_FALSE both require balancing.
template<class PlatformApi, class Work>
bool RunInComApartment(Work&& work)
{
    class Apartment
    {
    public:
        Apartment() : result(static_cast<std::int32_t>(PlatformApi::Initialize())) {}
        ~Apartment() { if (result >= 0) PlatformApi::Uninitialize(); }
        Apartment(const Apartment&) = delete;
        Apartment& operator=(const Apartment&) = delete;
        const std::int32_t result;
    } apartment;

    if (apartment.result < 0) return false;
    work();
    return true;
}
