using System;
using System.Collections.Generic;
using System.Text;

namespace Parkour.Text;

/// <summary>
/// Determines which direction to favor when translating positions
/// that fall within inserted/deleted regions.
/// </summary>
public enum PositionBias
{
    /// <summary>
    /// Translate positions to the left on insertion boundaries
    /// </summary>
    Left,

    /// <summary>
    /// Translate positions to the right on insertion boundaries.
    /// </summary>
    Right
}