#pragma once

#ifdef SORTINGLOGIC_CPP_EXPORTS
#define SORTING_API extern "C" __declspec(dllexport)
#else
#define SORTING_API extern "C" __declspec(dllimport)
#endif

typedef void(__stdcall* UpdateCallback)(const int* arr, int size, int activeIdx1, int activeIdx2);

enum SortingAlgorithm
{
    BubbleSort = 0,
    InsertionSort = 1,
    MergeSort = 2,
    QuickSort = 3,
    HeapSort = 4,
    RadixSort = 5,
    CountingSort = 6,
    IntroSort = 7,
    SelectionSort = 8,
    CocktailSort = 9,
    CombSort = 10,
    GnomeSort = 11,
    OddEvenSort = 12,
    ShellSort = 13,
    CycleSort = 14,
    PancakeSort = 15,
    ExchangeSort = 16,
    BinaryInsertionSort = 17,
    BitonicSort = 18,
    StoogeSort = 19,
    TournamentSort = 20,
    BucketSort = 21,
    PigeonholeSort = 22,
    BeadSort = 23,
    FlashSort = 24,
    TimSort = 25,
    TreeSort = 26,
    PatienceSort = 27,
    StrandSort = 28,
    DoubleSelectionSort = 29
};

SORTING_API void __stdcall StartSort(int* arr, int size, int algorithmCode, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds);
