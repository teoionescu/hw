
package common;
import java.text.SimpleDateFormat;
import java.util.Date;

public class ChatMessage implements Message
{
    public String mSource;
    public String mDestination;
    public String mBody;
    public Date mTimeSent;
    
    public ChatMessage() {
        this.mTimeSent = new Date();
        // DateFormat dateFormat = new SimpleDateFormat("yyyy/MM/dd HH:mm:ss");
        // dateFormat.format(date)
    }

    @Override
    public MsgType getType() {
        return MsgType.MSG_CHAT;
    }

    @Override
    public String stringFormat() {
        return String.format(
            "User %s tells you: \"%s\"",
            this.mSource,
            this.mBody
        );
    }
}
