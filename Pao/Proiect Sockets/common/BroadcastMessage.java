
package common;

public class BroadcastMessage extends ChatMessage
{
    public BroadcastMessage() {
        super();
    }

    @Override
    public MsgType getType() {
        return MsgType.MSG_BROADCAST;
    }

    @Override
    public String stringFormat() {
        return String.format(
            "User %s broadcast to everybody: \"%s\"",
            this.mSource,
            this.mBody
        );
    }
}
