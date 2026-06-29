#include "SortingAPI.h"

#include <algorithm>
#include <chrono>
#include <cmath>
#include <deque>
#include <iterator>
#include <list>
#include <limits>
#include <set>
#include <thread>
#include <vector>

namespace
{
    bool IsCancelled(volatile bool* cancelFlag)
    {
        return cancelFlag != nullptr && *cancelFlag;
    }

    void Notify(int* arr, int size, int activeIdx1, int activeIdx2, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        if (IsCancelled(cancelFlag))
        {
            return;
        }

        if (callback != nullptr)
        {
            callback(arr, size, activeIdx1, activeIdx2);
        }

        if (delayMilliseconds > 0)
        {
            std::this_thread::sleep_for(std::chrono::milliseconds(delayMilliseconds));
        }
    }

    void BubbleSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        for (int i = 0; i < size - 1 && !IsCancelled(cancelFlag); ++i)
        {
            for (int j = 0; j < size - i - 1 && !IsCancelled(cancelFlag); ++j)
            {
                Notify(arr, size, j, j + 1, callback, cancelFlag, delayMilliseconds);
                if (arr[j] > arr[j + 1])
                {
                    std::swap(arr[j], arr[j + 1]);
                    Notify(arr, size, j, j + 1, callback, cancelFlag, delayMilliseconds);
                }
            }
        }
    }

    void InsertionSortRange(int* arr, int left, int right, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        for (int i = left + 1; i <= right && !IsCancelled(cancelFlag); ++i)
        {
            int key = arr[i];
            int j = i - 1;

            while (j >= left && !IsCancelled(cancelFlag))
            {
                Notify(arr, size, j, j + 1, callback, cancelFlag, delayMilliseconds);
                if (arr[j] <= key)
                {
                    break;
                }

                arr[j + 1] = arr[j];
                Notify(arr, size, j, j + 1, callback, cancelFlag, delayMilliseconds);
                --j;
            }

            arr[j + 1] = key;
            Notify(arr, size, j + 1, i, callback, cancelFlag, delayMilliseconds);
        }
    }

    void InsertionSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        InsertionSortRange(arr, 0, size - 1, size, callback, cancelFlag, delayMilliseconds);
    }

    void Merge(int* arr, int left, int middle, int right, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        std::vector<int> leftValues(arr + left, arr + middle + 1);
        std::vector<int> rightValues(arr + middle + 1, arr + right + 1);
        int i = 0;
        int j = 0;
        int k = left;

        while (i < static_cast<int>(leftValues.size()) && j < static_cast<int>(rightValues.size()) && !IsCancelled(cancelFlag))
        {
            Notify(arr, size, left + i, middle + 1 + j, callback, cancelFlag, delayMilliseconds);
            if (leftValues[i] <= rightValues[j])
            {
                arr[k++] = leftValues[i++];
            }
            else
            {
                arr[k++] = rightValues[j++];
            }
            Notify(arr, size, k - 1, -1, callback, cancelFlag, delayMilliseconds);
        }

        while (i < static_cast<int>(leftValues.size()) && !IsCancelled(cancelFlag))
        {
            arr[k++] = leftValues[i++];
            Notify(arr, size, k - 1, -1, callback, cancelFlag, delayMilliseconds);
        }

        while (j < static_cast<int>(rightValues.size()) && !IsCancelled(cancelFlag))
        {
            arr[k++] = rightValues[j++];
            Notify(arr, size, k - 1, -1, callback, cancelFlag, delayMilliseconds);
        }
    }

    void MergeSortRange(int* arr, int left, int right, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        if (left >= right || IsCancelled(cancelFlag))
        {
            return;
        }

        int middle = left + (right - left) / 2;
        MergeSortRange(arr, left, middle, size, callback, cancelFlag, delayMilliseconds);
        MergeSortRange(arr, middle + 1, right, size, callback, cancelFlag, delayMilliseconds);
        Merge(arr, left, middle, right, size, callback, cancelFlag, delayMilliseconds);
    }

    int Partition(int* arr, int low, int high, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        int pivot = arr[high];
        int i = low - 1;

        for (int j = low; j < high && !IsCancelled(cancelFlag); ++j)
        {
            Notify(arr, size, j, high, callback, cancelFlag, delayMilliseconds);
            if (arr[j] < pivot)
            {
                ++i;
                std::swap(arr[i], arr[j]);
                Notify(arr, size, i, j, callback, cancelFlag, delayMilliseconds);
            }
        }

        std::swap(arr[i + 1], arr[high]);
        Notify(arr, size, i + 1, high, callback, cancelFlag, delayMilliseconds);
        return i + 1;
    }

    void QuickSortRange(int* arr, int low, int high, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        if (low >= high || IsCancelled(cancelFlag))
        {
            return;
        }

        int pivotIndex = Partition(arr, low, high, size, callback, cancelFlag, delayMilliseconds);
        QuickSortRange(arr, low, pivotIndex - 1, size, callback, cancelFlag, delayMilliseconds);
        QuickSortRange(arr, pivotIndex + 1, high, size, callback, cancelFlag, delayMilliseconds);
    }

    void Heapify(int* arr, int heapSize, int rootIndex, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        while (!IsCancelled(cancelFlag))
        {
            int largest = rootIndex;
            int left = 2 * rootIndex + 1;
            int right = 2 * rootIndex + 2;

            if (left < heapSize)
            {
                Notify(arr, size, largest, left, callback, cancelFlag, delayMilliseconds);
                if (arr[left] > arr[largest])
                {
                    largest = left;
                }
            }

            if (right < heapSize)
            {
                Notify(arr, size, largest, right, callback, cancelFlag, delayMilliseconds);
                if (arr[right] > arr[largest])
                {
                    largest = right;
                }
            }

            if (largest == rootIndex)
            {
                return;
            }

            std::swap(arr[rootIndex], arr[largest]);
            Notify(arr, size, rootIndex, largest, callback, cancelFlag, delayMilliseconds);
            rootIndex = largest;
        }
    }

    void HeapSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        for (int i = size / 2 - 1; i >= 0 && !IsCancelled(cancelFlag); --i)
        {
            Heapify(arr, size, i, size, callback, cancelFlag, delayMilliseconds);
        }

        for (int i = size - 1; i > 0 && !IsCancelled(cancelFlag); --i)
        {
            std::swap(arr[0], arr[i]);
            Notify(arr, size, 0, i, callback, cancelFlag, delayMilliseconds);
            Heapify(arr, i, 0, size, callback, cancelFlag, delayMilliseconds);
        }
    }

    void CountingSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        int minimumValue = *std::min_element(arr, arr + size);
        int maximumValue = *std::max_element(arr, arr + size);
        long long rangeLength = static_cast<long long>(maximumValue) - minimumValue + 1;

        if (rangeLength <= 0 || rangeLength > 1000000)
        {
            std::stable_sort(arr, arr + size);
            Notify(arr, size, -1, -1, callback, cancelFlag, delayMilliseconds);
            return;
        }

        std::vector<int> counts(static_cast<size_t>(rangeLength), 0);
        for (int i = 0; i < size && !IsCancelled(cancelFlag); ++i)
        {
            ++counts[arr[i] - minimumValue];
            Notify(arr, size, i, -1, callback, cancelFlag, delayMilliseconds);
        }

        int outputIndex = 0;
        for (int value = minimumValue; value <= maximumValue && !IsCancelled(cancelFlag); ++value)
        {
            int countIndex = value - minimumValue;
            while (counts[countIndex]-- > 0 && !IsCancelled(cancelFlag))
            {
                arr[outputIndex] = value;
                Notify(arr, size, outputIndex, -1, callback, cancelFlag, delayMilliseconds);
                ++outputIndex;
            }
        }
    }

    void RadixSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        int minimumValue = *std::min_element(arr, arr + size);
        std::vector<int> shiftedValues(size);
        int maximumShiftedValue = 0;

        for (int i = 0; i < size && !IsCancelled(cancelFlag); ++i)
        {
            shiftedValues[i] = arr[i] - minimumValue;
            maximumShiftedValue = std::max(maximumShiftedValue, shiftedValues[i]);
        }

        for (int exponent = 1; maximumShiftedValue / exponent > 0 && !IsCancelled(cancelFlag); exponent *= 10)
        {
            std::vector<int> output(size);
            int counts[10] = { 0 };

            for (int i = 0; i < size && !IsCancelled(cancelFlag); ++i)
            {
                ++counts[(shiftedValues[i] / exponent) % 10];
                Notify(arr, size, i, -1, callback, cancelFlag, delayMilliseconds);
            }

            for (int i = 1; i < 10; ++i)
            {
                counts[i] += counts[i - 1];
            }

            for (int i = size - 1; i >= 0 && !IsCancelled(cancelFlag); --i)
            {
                int digit = (shiftedValues[i] / exponent) % 10;
                output[--counts[digit]] = shiftedValues[i];
            }

            for (int i = 0; i < size && !IsCancelled(cancelFlag); ++i)
            {
                shiftedValues[i] = output[i];
                arr[i] = shiftedValues[i] + minimumValue;
                Notify(arr, size, i, -1, callback, cancelFlag, delayMilliseconds);
            }

            if (exponent > std::numeric_limits<int>::max() / 10)
            {
                break;
            }
        }
    }

    void IntroSortRange(int* arr, int low, int high, int depthLimit, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        const int insertionThreshold = 16;
        if (low >= high || IsCancelled(cancelFlag))
        {
            return;
        }

        if (high - low + 1 <= insertionThreshold)
        {
            InsertionSortRange(arr, low, high, size, callback, cancelFlag, delayMilliseconds);
            return;
        }

        if (depthLimit == 0)
        {
            std::make_heap(arr + low, arr + high + 1);
            std::sort_heap(arr + low, arr + high + 1);
            Notify(arr, size, low, high, callback, cancelFlag, delayMilliseconds);
            return;
        }

        int pivotIndex = Partition(arr, low, high, size, callback, cancelFlag, delayMilliseconds);
        IntroSortRange(arr, low, pivotIndex - 1, depthLimit - 1, size, callback, cancelFlag, delayMilliseconds);
        IntroSortRange(arr, pivotIndex + 1, high, depthLimit - 1, size, callback, cancelFlag, delayMilliseconds);
    }

    void IntroSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        int depthLimit = 2 * static_cast<int>(std::log2(size > 1 ? size : 2));
        IntroSortRange(arr, 0, size - 1, depthLimit, size, callback, cancelFlag, delayMilliseconds);
    }

    void WriteSortedValues(int* arr, int size, const std::vector<int>& sortedValues, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        for (int i = 0; i < size && !IsCancelled(cancelFlag); ++i)
        {
            arr[i] = sortedValues[i];
            Notify(arr, size, i, -1, callback, cancelFlag, delayMilliseconds);
        }
    }

    void SelectionSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        for (int i = 0; i < size - 1 && !IsCancelled(cancelFlag); ++i)
        {
            int minimumIndex = i;
            for (int j = i + 1; j < size && !IsCancelled(cancelFlag); ++j)
            {
                Notify(arr, size, minimumIndex, j, callback, cancelFlag, delayMilliseconds);
                if (arr[j] < arr[minimumIndex])
                {
                    minimumIndex = j;
                }
            }

            if (minimumIndex != i)
            {
                std::swap(arr[i], arr[minimumIndex]);
                Notify(arr, size, i, minimumIndex, callback, cancelFlag, delayMilliseconds);
            }
        }
    }

    void CocktailSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        bool swapped = true;
        int start = 0;
        int end = size - 1;

        while (swapped && !IsCancelled(cancelFlag))
        {
            swapped = false;
            for (int i = start; i < end && !IsCancelled(cancelFlag); ++i)
            {
                Notify(arr, size, i, i + 1, callback, cancelFlag, delayMilliseconds);
                if (arr[i] > arr[i + 1])
                {
                    std::swap(arr[i], arr[i + 1]);
                    swapped = true;
                    Notify(arr, size, i, i + 1, callback, cancelFlag, delayMilliseconds);
                }
            }

            if (!swapped)
            {
                break;
            }

            swapped = false;
            --end;
            for (int i = end - 1; i >= start && !IsCancelled(cancelFlag); --i)
            {
                Notify(arr, size, i, i + 1, callback, cancelFlag, delayMilliseconds);
                if (arr[i] > arr[i + 1])
                {
                    std::swap(arr[i], arr[i + 1]);
                    swapped = true;
                    Notify(arr, size, i, i + 1, callback, cancelFlag, delayMilliseconds);
                }
            }
            ++start;
        }
    }

    void CombSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        int gap = size;
        bool swapped = true;

        while ((gap != 1 || swapped) && !IsCancelled(cancelFlag))
        {
            gap = std::max(1, static_cast<int>(gap / 1.3));
            swapped = false;

            for (int i = 0; i + gap < size && !IsCancelled(cancelFlag); ++i)
            {
                Notify(arr, size, i, i + gap, callback, cancelFlag, delayMilliseconds);
                if (arr[i] > arr[i + gap])
                {
                    std::swap(arr[i], arr[i + gap]);
                    swapped = true;
                    Notify(arr, size, i, i + gap, callback, cancelFlag, delayMilliseconds);
                }
            }
        }
    }

    void GnomeSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        int index = 0;
        while (index < size && !IsCancelled(cancelFlag))
        {
            if (index == 0)
            {
                ++index;
                continue;
            }

            Notify(arr, size, index - 1, index, callback, cancelFlag, delayMilliseconds);
            if (arr[index] >= arr[index - 1])
            {
                ++index;
            }
            else
            {
                std::swap(arr[index], arr[index - 1]);
                Notify(arr, size, index - 1, index, callback, cancelFlag, delayMilliseconds);
                --index;
            }
        }
    }

    void OddEvenSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        bool sorted = false;
        while (!sorted && !IsCancelled(cancelFlag))
        {
            sorted = true;
            for (int i = 1; i < size - 1 && !IsCancelled(cancelFlag); i += 2)
            {
                Notify(arr, size, i, i + 1, callback, cancelFlag, delayMilliseconds);
                if (arr[i] > arr[i + 1])
                {
                    std::swap(arr[i], arr[i + 1]);
                    sorted = false;
                    Notify(arr, size, i, i + 1, callback, cancelFlag, delayMilliseconds);
                }
            }

            for (int i = 0; i < size - 1 && !IsCancelled(cancelFlag); i += 2)
            {
                Notify(arr, size, i, i + 1, callback, cancelFlag, delayMilliseconds);
                if (arr[i] > arr[i + 1])
                {
                    std::swap(arr[i], arr[i + 1]);
                    sorted = false;
                    Notify(arr, size, i, i + 1, callback, cancelFlag, delayMilliseconds);
                }
            }
        }
    }

    void ShellSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        for (int gap = size / 2; gap > 0 && !IsCancelled(cancelFlag); gap /= 2)
        {
            for (int i = gap; i < size && !IsCancelled(cancelFlag); ++i)
            {
                int value = arr[i];
                int j = i;
                while (j >= gap && !IsCancelled(cancelFlag))
                {
                    Notify(arr, size, j - gap, j, callback, cancelFlag, delayMilliseconds);
                    if (arr[j - gap] <= value)
                    {
                        break;
                    }

                    arr[j] = arr[j - gap];
                    Notify(arr, size, j - gap, j, callback, cancelFlag, delayMilliseconds);
                    j -= gap;
                }
                arr[j] = value;
                Notify(arr, size, j, i, callback, cancelFlag, delayMilliseconds);
            }
        }
    }

    void CycleSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        for (int cycleStart = 0; cycleStart <= size - 2 && !IsCancelled(cancelFlag); ++cycleStart)
        {
            int item = arr[cycleStart];
            int position = cycleStart;

            for (int i = cycleStart + 1; i < size && !IsCancelled(cancelFlag); ++i)
            {
                Notify(arr, size, cycleStart, i, callback, cancelFlag, delayMilliseconds);
                if (arr[i] < item)
                {
                    ++position;
                }
            }

            if (position == cycleStart)
            {
                continue;
            }

            while (position < size && item == arr[position])
            {
                ++position;
            }

            if (position < size)
            {
                std::swap(item, arr[position]);
                Notify(arr, size, cycleStart, position, callback, cancelFlag, delayMilliseconds);
            }

            while (position != cycleStart && !IsCancelled(cancelFlag))
            {
                position = cycleStart;
                for (int i = cycleStart + 1; i < size && !IsCancelled(cancelFlag); ++i)
                {
                    if (arr[i] < item)
                    {
                        ++position;
                    }
                }

                while (position < size && item == arr[position])
                {
                    ++position;
                }

                if (position < size)
                {
                    std::swap(item, arr[position]);
                    Notify(arr, size, cycleStart, position, callback, cancelFlag, delayMilliseconds);
                }
            }
        }
    }

    void ReversePrefix(int* arr, int endIndex, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        int startIndex = 0;
        while (startIndex < endIndex && !IsCancelled(cancelFlag))
        {
            std::swap(arr[startIndex], arr[endIndex]);
            Notify(arr, size, startIndex, endIndex, callback, cancelFlag, delayMilliseconds);
            ++startIndex;
            --endIndex;
        }
    }

    void PancakeSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        for (int currentSize = size; currentSize > 1 && !IsCancelled(cancelFlag); --currentSize)
        {
            int maximumIndex = static_cast<int>(std::max_element(arr, arr + currentSize) - arr);
            Notify(arr, size, maximumIndex, currentSize - 1, callback, cancelFlag, delayMilliseconds);
            if (maximumIndex == currentSize - 1)
            {
                continue;
            }

            ReversePrefix(arr, maximumIndex, size, callback, cancelFlag, delayMilliseconds);
            ReversePrefix(arr, currentSize - 1, size, callback, cancelFlag, delayMilliseconds);
        }
    }

    void ExchangeSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        for (int i = 0; i < size - 1 && !IsCancelled(cancelFlag); ++i)
        {
            for (int j = i + 1; j < size && !IsCancelled(cancelFlag); ++j)
            {
                Notify(arr, size, i, j, callback, cancelFlag, delayMilliseconds);
                if (arr[i] > arr[j])
                {
                    std::swap(arr[i], arr[j]);
                    Notify(arr, size, i, j, callback, cancelFlag, delayMilliseconds);
                }
            }
        }
    }

    int BinarySearchPosition(int* arr, int item, int low, int high)
    {
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            if (item == arr[middle])
            {
                return middle + 1;
            }
            if (item > arr[middle])
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }
        return low;
    }

    void BinaryInsertionSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        for (int i = 1; i < size && !IsCancelled(cancelFlag); ++i)
        {
            int item = arr[i];
            int position = BinarySearchPosition(arr, item, 0, i - 1);
            int j = i - 1;

            while (j >= position && !IsCancelled(cancelFlag))
            {
                arr[j + 1] = arr[j];
                Notify(arr, size, j, j + 1, callback, cancelFlag, delayMilliseconds);
                --j;
            }

            arr[position] = item;
            Notify(arr, size, position, i, callback, cancelFlag, delayMilliseconds);
        }
    }

    void BitonicCompare(int* arr, int low, int count, bool ascending, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        if (count <= 1 || IsCancelled(cancelFlag))
        {
            return;
        }

        int distance = count / 2;
        for (int i = low; i < low + distance && i + distance < size && !IsCancelled(cancelFlag); ++i)
        {
            Notify(arr, size, i, i + distance, callback, cancelFlag, delayMilliseconds);
            if ((ascending && arr[i] > arr[i + distance]) || (!ascending && arr[i] < arr[i + distance]))
            {
                std::swap(arr[i], arr[i + distance]);
                Notify(arr, size, i, i + distance, callback, cancelFlag, delayMilliseconds);
            }
        }

        BitonicCompare(arr, low, distance, ascending, size, callback, cancelFlag, delayMilliseconds);
        BitonicCompare(arr, low + distance, count - distance, ascending, size, callback, cancelFlag, delayMilliseconds);
    }

    void BitonicSortRange(int* arr, int low, int count, bool ascending, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        if (count <= 1 || IsCancelled(cancelFlag))
        {
            return;
        }

        int distance = count / 2;
        BitonicSortRange(arr, low, distance, true, size, callback, cancelFlag, delayMilliseconds);
        BitonicSortRange(arr, low + distance, count - distance, false, size, callback, cancelFlag, delayMilliseconds);
        BitonicCompare(arr, low, count, ascending, size, callback, cancelFlag, delayMilliseconds);
    }

    void BitonicSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        BitonicSortRange(arr, 0, size, true, size, callback, cancelFlag, delayMilliseconds);
        if (!std::is_sorted(arr, arr + size))
        {
            ShellSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        }
    }

    void StoogeSortRange(int* arr, int left, int right, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        if (left >= right || IsCancelled(cancelFlag))
        {
            return;
        }

        Notify(arr, size, left, right, callback, cancelFlag, delayMilliseconds);
        if (arr[left] > arr[right])
        {
            std::swap(arr[left], arr[right]);
            Notify(arr, size, left, right, callback, cancelFlag, delayMilliseconds);
        }

        if (right - left + 1 > 2)
        {
            int third = (right - left + 1) / 3;
            StoogeSortRange(arr, left, right - third, size, callback, cancelFlag, delayMilliseconds);
            StoogeSortRange(arr, left + third, right, size, callback, cancelFlag, delayMilliseconds);
            StoogeSortRange(arr, left, right - third, size, callback, cancelFlag, delayMilliseconds);
        }
    }

    void TournamentSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        std::vector<int> values(arr, arr + size);
        std::vector<int> sortedValues;
        sortedValues.reserve(size);

        while (!values.empty() && !IsCancelled(cancelFlag))
        {
            int minimumIndex = 0;
            for (int i = 1; i < static_cast<int>(values.size()) && !IsCancelled(cancelFlag); ++i)
            {
                if (values[i] < values[minimumIndex])
                {
                    minimumIndex = i;
                }
            }

            sortedValues.push_back(values[minimumIndex]);
            values.erase(values.begin() + minimumIndex);
        }

        WriteSortedValues(arr, size, sortedValues, callback, cancelFlag, delayMilliseconds);
    }

    void BucketSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        int minimumValue = *std::min_element(arr, arr + size);
        int maximumValue = *std::max_element(arr, arr + size);
        if (minimumValue == maximumValue)
        {
            Notify(arr, size, -1, -1, callback, cancelFlag, delayMilliseconds);
            return;
        }

        int bucketCount = std::max(1, static_cast<int>(std::sqrt(size)));
        std::vector<std::vector<int>> buckets(bucketCount);
        double range = static_cast<double>(maximumValue) - minimumValue + 1.0;

        for (int i = 0; i < size && !IsCancelled(cancelFlag); ++i)
        {
            int bucketIndex = std::min(bucketCount - 1, static_cast<int>(((arr[i] - minimumValue) / range) * bucketCount));
            buckets[bucketIndex].push_back(arr[i]);
            Notify(arr, size, i, -1, callback, cancelFlag, delayMilliseconds);
        }

        int outputIndex = 0;
        for (int i = 0; i < bucketCount && !IsCancelled(cancelFlag); ++i)
        {
            std::sort(buckets[i].begin(), buckets[i].end());
            for (int value : buckets[i])
            {
                if (IsCancelled(cancelFlag))
                {
                    return;
                }
                arr[outputIndex] = value;
                Notify(arr, size, outputIndex, -1, callback, cancelFlag, delayMilliseconds);
                ++outputIndex;
            }
        }
    }

    void PigeonholeSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        CountingSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
    }

    void BeadSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        int minimumValue = *std::min_element(arr, arr + size);
        if (minimumValue < 0)
        {
            CountingSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
            return;
        }

        int maximumValue = *std::max_element(arr, arr + size);
        if (maximumValue == 0 || static_cast<long long>(maximumValue) * size > 1000000)
        {
            CountingSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
            return;
        }

        std::vector<int> beads(static_cast<size_t>(maximumValue), 0);
        for (int i = 0; i < size && !IsCancelled(cancelFlag); ++i)
        {
            for (int j = 0; j < arr[i]; ++j)
            {
                ++beads[j];
            }
            Notify(arr, size, i, -1, callback, cancelFlag, delayMilliseconds);
        }

        for (int i = size - 1; i >= 0 && !IsCancelled(cancelFlag); --i)
        {
            int value = 0;
            for (int j = 0; j < maximumValue; ++j)
            {
                if (beads[j] > 0)
                {
                    ++value;
                    --beads[j];
                }
            }
            arr[i] = value;
            Notify(arr, size, i, -1, callback, cancelFlag, delayMilliseconds);
        }
    }

    void FlashSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        if (size <= 1)
        {
            return;
        }

        int minimumValue = *std::min_element(arr, arr + size);
        int maximumIndex = static_cast<int>(std::max_element(arr, arr + size) - arr);
        int maximumValue = arr[maximumIndex];
        if (minimumValue == maximumValue)
        {
            Notify(arr, size, -1, -1, callback, cancelFlag, delayMilliseconds);
            return;
        }

        int classCount = std::max(2, static_cast<int>(0.43 * size));
        std::vector<int> classes(classCount, 0);
        double scale = static_cast<double>(classCount - 1) / (maximumValue - minimumValue);

        for (int i = 0; i < size && !IsCancelled(cancelFlag); ++i)
        {
            ++classes[static_cast<int>(scale * (arr[i] - minimumValue))];
            Notify(arr, size, i, -1, callback, cancelFlag, delayMilliseconds);
        }

        for (int i = 1; i < classCount; ++i)
        {
            classes[i] += classes[i - 1];
        }

        std::swap(arr[maximumIndex], arr[0]);
        Notify(arr, size, maximumIndex, 0, callback, cancelFlag, delayMilliseconds);

        int moves = 0;
        int j = 0;
        int k = classCount - 1;
        while (moves < size - 1 && !IsCancelled(cancelFlag))
        {
            while (j > classes[k] - 1 && !IsCancelled(cancelFlag))
            {
                ++j;
                k = static_cast<int>(scale * (arr[j] - minimumValue));
            }

            int flash = arr[j];
            while (j != classes[k] && !IsCancelled(cancelFlag))
            {
                k = static_cast<int>(scale * (flash - minimumValue));
                int target = --classes[k];
                std::swap(flash, arr[target]);
                Notify(arr, size, j, target, callback, cancelFlag, delayMilliseconds);
                ++moves;
            }
        }

        InsertionSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
    }

    void TimSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        const int runSize = 32;
        for (int start = 0; start < size && !IsCancelled(cancelFlag); start += runSize)
        {
            int end = std::min(start + runSize - 1, size - 1);
            InsertionSortRange(arr, start, end, size, callback, cancelFlag, delayMilliseconds);
        }

        for (int mergeSize = runSize; mergeSize < size && !IsCancelled(cancelFlag); mergeSize *= 2)
        {
            for (int left = 0; left < size && !IsCancelled(cancelFlag); left += 2 * mergeSize)
            {
                int middle = std::min(left + mergeSize - 1, size - 1);
                int right = std::min(left + 2 * mergeSize - 1, size - 1);
                if (middle < right)
                {
                    Merge(arr, left, middle, right, size, callback, cancelFlag, delayMilliseconds);
                }
            }
        }
    }

    void TreeSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        std::multiset<int> treeValues;
        for (int i = 0; i < size && !IsCancelled(cancelFlag); ++i)
        {
            treeValues.insert(arr[i]);
            Notify(arr, size, i, -1, callback, cancelFlag, delayMilliseconds);
        }

        int index = 0;
        for (int value : treeValues)
        {
            if (IsCancelled(cancelFlag))
            {
                return;
            }
            arr[index] = value;
            Notify(arr, size, index, -1, callback, cancelFlag, delayMilliseconds);
            ++index;
        }
    }

    void PatienceSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        std::vector<std::vector<int>> piles;
        for (int i = 0; i < size && !IsCancelled(cancelFlag); ++i)
        {
            int value = arr[i];
            auto pile = std::lower_bound(piles.begin(), piles.end(), value, [](const std::vector<int>& currentPile, int item)
            {
                return currentPile.back() < item;
            });

            if (pile == piles.end())
            {
                piles.push_back(std::vector<int>(1, value));
            }
            else
            {
                pile->push_back(value);
            }
            Notify(arr, size, i, -1, callback, cancelFlag, delayMilliseconds);
        }

        std::vector<int> sortedValues;
        sortedValues.reserve(size);
        while (!piles.empty() && !IsCancelled(cancelFlag))
        {
            int minimumPile = 0;
            for (int i = 1; i < static_cast<int>(piles.size()); ++i)
            {
                if (piles[i].back() < piles[minimumPile].back())
                {
                    minimumPile = i;
                }
            }

            sortedValues.push_back(piles[minimumPile].back());
            piles[minimumPile].pop_back();
            if (piles[minimumPile].empty())
            {
                piles.erase(piles.begin() + minimumPile);
            }
        }

        WriteSortedValues(arr, size, sortedValues, callback, cancelFlag, delayMilliseconds);
    }

    void StrandSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        std::list<int> inputValues(arr, arr + size);
        std::vector<int> sortedValues;

        while (!inputValues.empty() && !IsCancelled(cancelFlag))
        {
            std::vector<int> strand;
            strand.push_back(inputValues.front());
            inputValues.pop_front();

            for (auto iterator = inputValues.begin(); iterator != inputValues.end() && !IsCancelled(cancelFlag);)
            {
                if (*iterator >= strand.back())
                {
                    strand.push_back(*iterator);
                    iterator = inputValues.erase(iterator);
                }
                else
                {
                    ++iterator;
                }
            }

            std::vector<int> mergedValues;
            std::merge(sortedValues.begin(), sortedValues.end(), strand.begin(), strand.end(), std::back_inserter(mergedValues));
            sortedValues.swap(mergedValues);
            for (int i = 0; i < static_cast<int>(sortedValues.size()) && !IsCancelled(cancelFlag); ++i)
            {
                arr[i] = sortedValues[i];
                Notify(arr, size, i, -1, callback, cancelFlag, delayMilliseconds);
            }
        }

        WriteSortedValues(arr, size, sortedValues, callback, cancelFlag, delayMilliseconds);
    }

    void DoubleSelectionSortInternal(int* arr, int size, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
    {
        int left = 0;
        int right = size - 1;

        while (left < right && !IsCancelled(cancelFlag))
        {
            int minimumIndex = left;
            int maximumIndex = right;

            if (arr[minimumIndex] > arr[maximumIndex])
            {
                std::swap(arr[minimumIndex], arr[maximumIndex]);
                Notify(arr, size, minimumIndex, maximumIndex, callback, cancelFlag, delayMilliseconds);
            }

            for (int i = left + 1; i < right && !IsCancelled(cancelFlag); ++i)
            {
                Notify(arr, size, i, minimumIndex, callback, cancelFlag, delayMilliseconds);
                if (arr[i] < arr[minimumIndex])
                {
                    minimumIndex = i;
                }

                Notify(arr, size, i, maximumIndex, callback, cancelFlag, delayMilliseconds);
                if (arr[i] > arr[maximumIndex])
                {
                    maximumIndex = i;
                }
            }

            if (minimumIndex != left)
            {
                std::swap(arr[left], arr[minimumIndex]);
                Notify(arr, size, left, minimumIndex, callback, cancelFlag, delayMilliseconds);
                if (maximumIndex == left)
                {
                    maximumIndex = minimumIndex;
                }
            }

            if (maximumIndex != right)
            {
                std::swap(arr[right], arr[maximumIndex]);
                Notify(arr, size, right, maximumIndex, callback, cancelFlag, delayMilliseconds);
            }

            ++left;
            --right;
        }
    }
}

SORTING_API void __stdcall StartSort(int* arr, int size, int algorithmCode, UpdateCallback callback, volatile bool* cancelFlag, int delayMilliseconds)
{
    if (arr == nullptr || size <= 0 || IsCancelled(cancelFlag))
    {
        return;
    }

    switch (algorithmCode)
    {
    case BubbleSort:
        BubbleSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case InsertionSort:
        InsertionSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case MergeSort:
        MergeSortRange(arr, 0, size - 1, size, callback, cancelFlag, delayMilliseconds);
        break;
    case QuickSort:
        QuickSortRange(arr, 0, size - 1, size, callback, cancelFlag, delayMilliseconds);
        break;
    case HeapSort:
        HeapSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case RadixSort:
        RadixSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case CountingSort:
        CountingSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case IntroSort:
        IntroSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case SelectionSort:
        SelectionSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case CocktailSort:
        CocktailSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case CombSort:
        CombSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case GnomeSort:
        GnomeSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case OddEvenSort:
        OddEvenSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case ShellSort:
        ShellSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case CycleSort:
        CycleSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case PancakeSort:
        PancakeSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case ExchangeSort:
        ExchangeSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case BinaryInsertionSort:
        BinaryInsertionSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case BitonicSort:
        BitonicSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case StoogeSort:
        StoogeSortRange(arr, 0, size - 1, size, callback, cancelFlag, delayMilliseconds);
        break;
    case TournamentSort:
        TournamentSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case BucketSort:
        BucketSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case PigeonholeSort:
        PigeonholeSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case BeadSort:
        BeadSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case FlashSort:
        FlashSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case TimSort:
        TimSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case TreeSort:
        TreeSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case PatienceSort:
        PatienceSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case StrandSort:
        StrandSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    case DoubleSelectionSort:
        DoubleSelectionSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    default:
        BubbleSortInternal(arr, size, callback, cancelFlag, delayMilliseconds);
        break;
    }

    Notify(arr, size, -1, -1, callback, cancelFlag, 0);
}
