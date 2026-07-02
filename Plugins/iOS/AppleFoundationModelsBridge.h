#pragma once

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef void (*AFM_EventCallback)(const char* eventJson);

void AFM_SetEventCallback(AFM_EventCallback callback);
void AFM_SetDebugLogging(uint8_t enabled);
void AFM_GetAvailability(const char* requestId);
void AFM_GenerateText(
    const char* requestId,
    const char* prompt,
    const char* optionsJson);
void AFM_StreamText(
    const char* requestId,
    const char* prompt,
    const char* optionsJson);
void AFM_CancelRequest(const char* requestId);

#ifdef __cplusplus
}
#endif
