# Hydro Data

Use this folder for offshore hydrodynamic files.

Depending on the OpenFAST/HydroDyn setup, this may include:

- WAMIT-style hydrodynamic coefficient files.
- HydroDyn-related precomputed data.
- `hydro.h5` files if your preprocessing chain uses them.

MOST's HybridSpar example uses `hydroData/HybridSpar/hydro.h5` for WEC-Sim/MOST hydrodynamics. OpenFAST HydroDyn has its own accepted file formats, so keep the files your OpenFAST case actually references here and point to them from the module input files.

