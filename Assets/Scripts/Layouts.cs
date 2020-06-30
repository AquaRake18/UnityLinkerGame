﻿using UnityEngine;

public static class Layouts {
    public static Vector2 _BoardTileSize = new Vector2(.5f, .5f);

    public static Vector2 GetBoardPos(Settings settings) {
        return new Vector2(
            _BoardTileSize.x / 2f * (settings._MaxWidth - settings._BoardWidth),
            .5f + _BoardTileSize.y / 2f * (settings._MaxHeight - settings._BoardHeight)
        );
    }
}
