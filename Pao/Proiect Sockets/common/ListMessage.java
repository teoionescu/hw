
package common;

public class ListMessage implements Message
{   
    public ListMessage() { }

    @Override
    public MsgType getType() {
        return MsgType.MSG_LIST;
    }

    @Override
    public String stringFormat() {
        return "Requesting online users list";
    }
}