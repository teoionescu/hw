
package common;
import java.io.Serializable;

public interface Message extends Serializable
{
    MsgType getType();
    String stringFormat();
}
