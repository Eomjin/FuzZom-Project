using ExitGames.Client.Photon;
using UnityEngine;

public class ColorSerialization {
    [System.Obsolete]
    public static byte[] SerializeColor(object targetObject) {
        Color color = (Color) targetObject;

        Quaternion colorToQuaterinon = new Quaternion(color.r, color.g, color.b, color.a);
        byte[] bytes = Protocol.Serialize(colorToQuaterinon);

        return bytes;
    }

    [System.Obsolete]
    public static object DeserializeColor(byte[] bytes) {
        Quaternion quaterinon = (Quaternion) Protocol.Deserialize(bytes);

        Color color = new Color(quaterinon.x, quaterinon.y, quaterinon.z, quaterinon.w);

        return color;
    }
}