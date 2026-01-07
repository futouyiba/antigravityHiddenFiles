# Data Lookup Implementation Plan

## Goal Description
Implement data lookup logic in `0precomputeDemo.ipynb` to assemble fish configuration data into numpy arrays/pandas DataFrames for downstream calculation. 

## Proposed Changes
### Notebook: `0precomputeDemo.ipynb`
### Notebook: `0precomputeDemo.ipynb`
- [ ] Load configuration files:
    - [NEW] `stock_release.json` (Stock -> Release mapping)
    - [NEW] `fish_release.json` (Release details)
    - [NEW] `basic_fish_quality.json` (Fish species & quality info)
    - [EXISTING] `fish_stock.json`
- [ ] Implement Data Lookup & Assembly:
    - Map `stockId` to `releaseId` using `stock_release.json`.
    - Retrieve fish details (fishId, envId) from `fish_release.json`.
    - Enrich with species and quality data (length, weight, etc.) from `basic_fish_quality.json`.
- [ ] Create `stockFishesPd` DataFrame with columns:
    - `stockId`, `releaseId`, `fishId`, `envId`, `species`, `qualityId`
    - `weight_min`, `weight_max`, `len_min`, `len_max`

## Verification Plan
### Automated Tests
- Run the notebook cells and verify the output `stockFishesPd` contains expected columns and data types.
