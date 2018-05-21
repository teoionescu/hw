
package common;
import java.io.Serializable;
import java.text.SimpleDateFormat;
import java.util.Date;

public class DisconnectMessage implements Message
{
    public DisconnectMessage() { }

    @Override
    public MsgType getType() {
        return MsgType.MSG_DISCONNECT;
    }
    
    @Override
    public String stringFormat() {
        return "Requesting disconnect";
    }
}